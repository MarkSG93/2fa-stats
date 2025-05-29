using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stats2fa.api;
using Stats2fa.api.models;
using Stats2fa.cache;
using Stats2fa.database;
using Stats2fa.logger;
using Stats2fa.tasks;
using StatsBetter.Output;

namespace Stats2fa;

internal class Program {
    private static ILogger _logger;

    // Method to create JSON serializer options that include null values
    private static JsonSerializerOptions CreateJsonOptionsWithNulls() {
        return new JsonSerializerOptions {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IgnoreReadOnlyProperties = false,
            WriteIndented = false
        };
    }

    private static async Task<int> Main(string[] args) {
        // Create the logger factory and logger
        using var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole().SetMinimumLevel(level: LogLevel.Information); });
        _logger = loggerFactory.CreateLogger<Program>(); // Assign logger to static field

        // Initialize PacketLogger with the logger
        _logger.LogInformation("Stats2fa starting");
        StatsLogger.InitializeLogger(logger: _logger);

        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true)
            .AddEnvironmentVariables()
            .AddCommandLine(args: args)
            .Build();

        var environment = "eu1";
        var reportDate = DateTime.UtcNow.Date;
        string apiKey = null, baseAddress = null, dbFileName = null, outputFileName = null;

        if (!string.IsNullOrEmpty(config["env"])) environment = config["env"];
        StatsLogger.Log(null, $"arguments supplied date={config["date"]}");

        if (string.IsNullOrEmpty(config["date"])) {
            StatsLogger.Log(null, $"Warning: No date specified in arguments. Using current UTC date: {reportDate:yyyy-MM-dd}", environment: environment);
        }
        else {
            reportDate = DateTime.ParseExact(config["date"], "yyyy-MM-dd", provider: CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            StatsLogger.Log(null, $"Date specified: {reportDate:yyyy-MM-dd}", environment: environment);
        }

        switch (environment) {
            case "eu1":
                apiKey = config["API_EU1"]!;
                baseAddress = config["ApiStrings:eu1"]!;
                dbFileName = config["DatabaseConfiguration:eu1"]!;
                outputFileName = Path.Combine(Environment.GetFolderPath(folder: Environment.SpecialFolder.MyDocuments), $"{config["DatabaseConfiguration:eu1"]!}_{DateTime.UtcNow:yyyy-M-d-hh-mm-ss}.json");
                break;
            case "us1":
                apiKey = config["API_US1"]!;
                baseAddress = config["ApiStrings:us1"]!;
                dbFileName = config["DatabaseConfiguration:us1"]!;
                outputFileName = Path.Combine(Environment.GetFolderPath(folder: Environment.SpecialFolder.MyDocuments), $"{config["DatabaseConfiguration:us1"]!}_{DateTime.UtcNow:yyyy-M-d-hh-mm-ss}.json");
                break;
            case "uk1":
                apiKey = config["API_UK1"]!;
                baseAddress = config["ApiStrings:uk1"]!;
                dbFileName = config["DatabaseConfiguration:uk1"]!;
                outputFileName = Path.Combine(Environment.GetFolderPath(folder: Environment.SpecialFolder.MyDocuments), $"{config["DatabaseConfiguration:uk1"]!}_{DateTime.UtcNow:yyyy-M-d-hh-mm-ss}.json");
                break;
            case "staging":
                apiKey = config["API_STAGING"]!;
                baseAddress = config["ApiStrings:staging"]!;
                dbFileName = config["DatabaseConfiguration:staging"]!;
                outputFileName = Path.Combine(Environment.GetFolderPath(folder: Environment.SpecialFolder.MyDocuments), $"{config["DatabaseConfiguration:staging"]!}_{DateTime.UtcNow:yyyy-M-d-hh-mm-ss}.json");
                break;
        }

        //  On macOS, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) typically returns the user's home directory (/Users/username) rather than the Documents folder.
        // This behavior is because .NET on macOS maps SpecialFolder.MyDocuments to the user's home directory, not a "Documents" subdirectory.
        // This is a platform-specific implementation detail of .NET Core on macOS.
        var dbPath = Path.Combine(Environment.GetFolderPath(folder: Environment.SpecialFolder.MyDocuments), $"{dbFileName}.db");

        var apiInformation = new ApiInformation {
            LastUpdated = DateTime.UtcNow,
            Environment = environment,
            Distributors = 0,
            Vendors = 0,
            Clients = 0,
            ApiCallsDistributors = 0,
            ApiCallsVendors = 0,
            ApiCallsClients = 0
        };

        // Delete existing database if exists and deleteDatabase parameter is set
        if (!string.IsNullOrEmpty(config["deleteDatabase"]) && config["deleteDatabase"].Equals("true") && File.Exists(path: dbPath)) {
            StatsLogger.Log(stats: apiInformation, $"Deleting existing database at {dbPath}");
            File.Delete(path: dbPath);
        }

        await using var db = new StatsContext(Environment.GetFolderPath(folder: Environment.SpecialFolder.MyDocuments), $"{dbFileName}.db", apiInformation: apiInformation);

        // Set the StatsContext reference in ApiInformation for user data saving
        apiInformation.StatsContext = db;

        // Ensure database is created with current schema
        StatsLogger.Log(stats: apiInformation, "Ensuring database is created");
        await db.Database.EnsureCreatedAsync();

        if (reportDate > DateTime.UtcNow) {
            StatsLogger.Log(stats: apiInformation, $"Deferring Execution Requested Date {reportDate} is still {(reportDate - DateTime.UtcNow).TotalHours:0000.00} hours in the future.");
            return (int)ExitCode.TooEarly;
        }


        // Step 0 Setup HttpClient
        StatsLogger.Log(stats: apiInformation, "Setting up HTTP client");
        var slidingWindowRateLimiterOptions = new SlidingWindowRateLimiterOptions {
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
            AutoReplenishment = true,
            PermitLimit = 4, // requests per window,
            // 4 has been found to be the fastest with minor errors
            // 3 has been to be the fastest with no errors
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10000
        };

        var rateLimiter = new SlidingWindowRateLimiter(options: slidingWindowRateLimiterOptions);

        using var httpClient = new HttpClient(new RetryHandler(new ClientSideRateLimitedHandler(limiter: rateLimiter)));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Add("x-api-key", value: apiKey);
        httpClient.Timeout = TimeSpan.FromSeconds(240); // fml vendor has 8004 clients
        httpClient.BaseAddress = new Uri(uriString: baseAddress);

        try {
            // Step 1 Fetch the Distributors
            StatsLogger.Log(stats: apiInformation, "Fetching distributors");
            var distributors = await ApiUtils.GetDistributors(client: httpClient, apiInformation: apiInformation, null, Convert.ToInt32(config["ApiQueryLimits:DistributorLimit"]), Convert.ToInt32(config["ApiQueryLimits:DistributorMax"]));
            StatsLogger.Log(stats: apiInformation, $"Total distributors ({distributors.Count:00000})");
            await Cache.SaveDistributors(db: db, distributors: distributors, reportDate: reportDate, apiInformation: apiInformation);

            if (distributors.DistributorList.Count == 0) {
                StatsLogger.Log(stats: apiInformation, "No distributors found. Exiting.");
                return (int)ExitCode.Success;
            }

            // Step 2 Fetch the Vendors
            StatsLogger.Log(stats: apiInformation, "Fetching vendors");
            var allVendors = new ConcurrentBag<Vendor>();
            try {
                await Task.WhenAll(Parallel.ForEachAsync(source: distributors.DistributorList,
                    (distributor, cancellationToken) =>
                        ApiUtils.GetVendorsForDistributor(result: allVendors,
                            httpClient: httpClient,
                            apiInformation: apiInformation,
                            distributor: distributor,
                            null,
                            cancellationToken: cancellationToken,
                            Convert.ToInt32(config["ApiQueryLimits:VendorLimit"]),
                            Convert.ToInt32(config["ApiQueryLimits:VendorMax"]))));
            }
            catch (Exception ex) {
                StatsLogger.Log(stats: apiInformation, $"Error during vendor fetching: {ex.Message}");
                // Continue with the vendors we were able to fetch
            }

            StatsLogger.Log(stats: apiInformation, $"Total vendors ({allVendors.Count:00000})");
            await Cache.SaveVendors(db: db, allVendors: allVendors, reportDate: reportDate, apiInformation: apiInformation);

            if (allVendors.Count == 0) {
                StatsLogger.Log(stats: apiInformation, "No vendors found. Exiting.");
                return (int)ExitCode.Success;
            }

            // Step 3 Fetch the Clients
            StatsLogger.Log(stats: apiInformation, "Fetching clients");
            var allClients = new ConcurrentBag<Client>();
            try {
                await Task.WhenAll(Parallel.ForEachAsync(source: allVendors,
                    (vendor, cancellationToken) =>
                        ApiUtils.GetClientsForVendor(result: allClients,
                            httpClient: httpClient,
                            apiInformation: apiInformation,
                            vendor: vendor,
                            null,
                            cancellationToken: cancellationToken,
                            Convert.ToInt32(config["ApiQueryLimits:ClientLimit"]),
                            Convert.ToInt32(config["ApiQueryLimits:ClientMax"]))));
            }
            catch (Exception ex) {
                StatsLogger.Log(stats: apiInformation, $"Error during client fetching: {ex.Message}");
                // Continue with the clients we were able to fetch
            }

            StatsLogger.Log(stats: apiInformation, $"Total clients ({allClients.Count:00000})");
            await Cache.SaveClients(db: db, allClients: allClients, reportDate: reportDate, apiInformation: apiInformation);

            // Step 4 Fetch the Distributor Information
            StatsLogger.Log(stats: apiInformation, "Fetching distributor information");
            await DistributorTasks.PopulateDistributorInformation(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate);

            // Step 5 Fetch the Vendor Information
            StatsLogger.Log(stats: apiInformation, "Fetching vendor information");
            await VendorTasks.PopulateVendorInformation(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate);

            // Step 6 Fetch the Client Information
            StatsLogger.Log(stats: apiInformation, "Fetching client information");
            await ClientTasks.PopulateClientInformation(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, Convert.ToInt32(config["ApiQueryLimits:ClientMax"]));
            // Step 7 Fetch the Users for Distributors
            StatsLogger.Log(stats: apiInformation, "Fetching users for Distributors");
            var allUsers = new ConcurrentBag<User>();
            try {
                var distributorIds = distributors.DistributorList.Select(x => x.Id).ToList();
                distributorIds.Add("00000000-0000-0000-0000-000000000000"); // add the system userId to the list of users to fetch
                await Task.WhenAll(Parallel.ForEachAsync(source: distributorIds,
                    (ownerId, cancellationToken) =>
                        ApiUtils.GetUsersForOwner(result: allUsers,
                            httpClient: httpClient,
                            apiInformation: apiInformation,
                            ownerId: ownerId,
                            null,
                            cancellationToken: cancellationToken,
                            Convert.ToInt32(config["ApiQueryLimits:ClientLimit"]),
                            Convert.ToInt32(config["ApiQueryLimits:ClientMax"]))));
            }
            catch (Exception ex) {
                StatsLogger.Log(stats: apiInformation, $"Error during fetching users for Distributors: {ex.Message}");
                // Continue with the users we were able to fetch
            }

            StatsLogger.Log(stats: apiInformation, $"Total Users from Distributors ({allUsers.Count:00000})");
            await Cache.SaveUsers(db: db, allUsers: allUsers, reportDate: reportDate, apiInformation: apiInformation);

            // Step 8 Fetch the Users for Vendors
            StatsLogger.Log(stats: apiInformation, "Fetching users for Vendors");
            var vendorUsers = new ConcurrentBag<User>();
            try {
                var vendorIds = allVendors.Select(x => x.Id).ToList();
                await Task.WhenAll(Parallel.ForEachAsync(source: vendorIds,
                    (ownerId, cancellationToken) =>
                        ApiUtils.GetUsersForOwner(result: vendorUsers,
                            httpClient: httpClient,
                            apiInformation: apiInformation,
                            ownerId: ownerId,
                            null,
                            cancellationToken: cancellationToken,
                            Convert.ToInt32(config["ApiQueryLimits:ClientLimit"]),
                            Convert.ToInt32(config["ApiQueryLimits:ClientMax"]))));
            }
            catch (Exception ex) {
                StatsLogger.Log(stats: apiInformation, $"Error during fetching users for Vendors: {ex.Message}");
                // Continue with the users we were able to fetch
            }

            StatsLogger.Log(stats: apiInformation, $"Total Users from Vendors ({vendorUsers.Count:00000})");
            await Cache.SaveUsers(db: db, allUsers: vendorUsers, reportDate: reportDate, apiInformation: apiInformation);

            // Step 9 Fetch the Users for Clients
            StatsLogger.Log(stats: apiInformation, "Fetching users for Clients");
            var clientUsers = new ConcurrentBag<User>();
            try {
                var clientIds = allClients.Select(x => x.Id).ToList();
                await Task.WhenAll(Parallel.ForEachAsync(source: clientIds,
                    (ownerId, cancellationToken) =>
                        ApiUtils.GetUsersForOwner(result: clientUsers,
                            httpClient: httpClient,
                            apiInformation: apiInformation,
                            ownerId: ownerId,
                            null,
                            cancellationToken: cancellationToken,
                            Convert.ToInt32(config["ApiQueryLimits:ClientLimit"]),
                            Convert.ToInt32(config["ApiQueryLimits:ClientMax"]))));
            }
            catch (Exception ex) {
                StatsLogger.Log(stats: apiInformation, $"Error during fetching users for Clients: {ex.Message}");
                // Continue with the users we were able to fetch
            }

            StatsLogger.Log(stats: apiInformation, $"Total Users from Clients ({clientUsers.Count:00000})");
            await Cache.SaveUsers(db: db, allUsers: clientUsers, reportDate: reportDate, apiInformation: apiInformation);

            // Step 10 Fetch the UserInformation for all Clients
            StatsLogger.Log(stats: apiInformation, "Fetching user information");
            await UserTasks.PopulateUserInformation(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, Convert.ToInt32(config["ApiQueryLimits:ClientMax"]));

            // Step 11 Write out the JSON File for Client,Distributor,Vendor Information

            // First, fetch all the info from the database
            List<ClientInformation> clientInformation = ClientTasks.FetchAllProcessedClients(db, reportDate, apiInformation: apiInformation);
            List<VendorInformation> vendorInformation = VendorTasks.FetchAllProcessedVendors(db, reportDate, apiInformation: apiInformation);
            List<DistributorInformation> distributorInformation = DistributorTasks.FetchAllProcessedDistributors(db, reportDate, apiInformation: apiInformation);

            // Make a mega object
            var multipleJsonLines = new List<string>();
            foreach (var client in clientInformation) {
                var vendor = vendorInformation.Single(x => x.VendorId == client.ClientVendorId);
                var distributor = distributorInformation.Single(x => x.DistributorId == vendor.VendorDistributorId);
                var stat = new StatsInformationDistributorVendorClient();

                stat.UserCreatedTimestamp = client.CreatedTimestamp;
                stat.ClientId = client.ClientId;
                stat.ClientName = client.ClientName;
                stat.ClientType = client.ClientType;
                stat.ClientStatus = client.ClientStatus;
                stat.ClientVendorId = client.ClientVendorId;
                stat.ClientStatsStatus = client.ClientStatsStatus;
                stat.ClientPasswordPolicySourceId = client.ClientPasswordPolicySourceId;
                stat.ClientPasswordPolicySourceName = client.ClientPasswordPolicySourceName;
                stat.ClientPasswordPolicySourceType = client.ClientPasswordPolicySourceType;
                stat.ClientPasswordPolicyPasswordLength = client.ClientPasswordPolicyPasswordLength;
                stat.ClientPasswordPolicyPasswordComplexityMixedcase = client.ClientPasswordPolicyPasswordComplexityMixedcase;
                stat.ClientPasswordPolicyPasswordComplexityAlphanumerical = client.ClientPasswordPolicyPasswordComplexityAlphanumerical;
                stat.ClientPasswordPolicyPasswordComplexityNocommonpasswords = client.ClientPasswordPolicyPasswordComplexityNocommonpasswords;
                stat.ClientPasswordPolicyPasswordComplexitySpecialcharacters = client.ClientPasswordPolicyPasswordComplexitySpecialcharacters;
                stat.ClientPasswordPolicyPasswordExpirationDays = client.ClientPasswordPolicyPasswordExpirationDays;
                stat.ClientPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = client.ClientPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays;
                stat.ClientPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = client.ClientPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays;
                stat.ClientPasswordPolicyOtpSettingsGracePeriodDays = client.ClientPasswordPolicyOtpSettingsGracePeriodDays;
                stat.ClientPasswordPolicyOtpSettingsMandatoryFor = client.ClientPasswordPolicyOtpSettingsMandatoryFor;

                stat.VendorCreatedTimestamp = vendor.CreatedTimestamp;
                stat.VendorDistributorId = vendor.VendorDistributorId;
                stat.VendorId = vendor.VendorId;
                stat.VendorName = vendor.VendorName;
                stat.VendorType = vendor.VendorType;
                stat.VendorStatus = vendor.VendorStatus;
                stat.VendorPasswordPolicySourceId = vendor.VendorPasswordPolicySourceId;
                stat.VendorPasswordPolicySourceName = vendor.VendorPasswordPolicySourceName;
                stat.VendorPasswordPolicySourceType = vendor.VendorPasswordPolicySourceType;
                stat.VendorPasswordPolicyPasswordLength = vendor.VendorPasswordPolicyPasswordLength;
                stat.VendorPasswordPolicyPasswordComplexityMixedcase = vendor.VendorPasswordPolicyPasswordComplexityMixedcase;
                stat.VendorPasswordPolicyPasswordComplexityAlphanumerical = vendor.VendorPasswordPolicyPasswordComplexityAlphanumerical;
                stat.VendorPasswordPolicyPasswordComplexityNocommonpasswords = vendor.VendorPasswordPolicyPasswordComplexityNocommonpasswords;
                stat.VendorPasswordPolicyPasswordComplexitySpecialcharacters = vendor.VendorPasswordPolicyPasswordComplexitySpecialcharacters;
                stat.VendorPasswordPolicyPasswordExpirationDays = vendor.VendorPasswordPolicyPasswordExpirationDays;
                stat.VendorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = vendor.VendorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays;
                stat.VendorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = vendor.VendorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays;
                stat.VendorPasswordPolicyOtpSettingsGracePeriodDays = vendor.VendorPasswordPolicyOtpSettingsGracePeriodDays;
                stat.VendorPasswordPolicyOtpSettingsMandatoryFor = vendor.VendorPasswordPolicyOtpSettingsMandatoryFor;

                stat.DistributorCreatedTimestamp = distributor.CreatedTimestamp;
                stat.DistributorId = distributor.DistributorId;
                stat.DistributorName = distributor.DistributorName;
                stat.DistributorType = distributor.DistributorType;
                stat.DistributorStatus = distributor.DistributorStatus;
                stat.DistributorPasswordPolicySourceId = distributor.DistributorPasswordPolicySourceId;
                stat.DistributorPasswordPolicySourceName = distributor.DistributorPasswordPolicySourceName;
                stat.DistributorPasswordPolicySourceType = distributor.DistributorPasswordPolicySourceType;
                stat.DistributorPasswordPolicyPasswordLength = distributor.DistributorPasswordPolicyPasswordLength;
                stat.DistributorPasswordPolicyPasswordComplexityMixedcase = distributor.DistributorPasswordPolicyPasswordComplexityMixedcase;
                stat.DistributorPasswordPolicyPasswordComplexityAlphanumerical = distributor.DistributorPasswordPolicyPasswordComplexityAlphanumerical;
                stat.DistributorPasswordPolicyPasswordComplexityNocommonpasswords = distributor.DistributorPasswordPolicyPasswordComplexityNocommonpasswords;
                stat.DistributorPasswordPolicyPasswordComplexitySpecialcharacters = distributor.DistributorPasswordPolicyPasswordComplexitySpecialcharacters;
                stat.DistributorPasswordPolicyPasswordExpirationDays = distributor.DistributorPasswordPolicyPasswordExpirationDays;
                stat.DistributorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = distributor.DistributorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays;
                stat.DistributorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = distributor.DistributorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays;
                stat.DistributorPasswordPolicyOtpSettingsGracePeriodDays = distributor.DistributorPasswordPolicyOtpSettingsGracePeriodDays;
                stat.DistributorPasswordPolicyOtpSettingsMandatoryFor = distributor.DistributorPasswordPolicyOtpSettingsMandatoryFor;
                stat.DistributorStatsStatus = distributor.DistributorStatsStatus;

                // Serialize with options that include null values
                multipleJsonLines.Add(JsonSerializer.Serialize(stat, CreateJsonOptionsWithNulls()));
            }

            // Write the output file
            StatsLogger.Log(stats: apiInformation, $"Writing output to {outputFileName}");
            await File.WriteAllLinesAsync(outputFileName, multipleJsonLines);
            StatsLogger.Log(stats: apiInformation, $"Successfully wrote {multipleJsonLines.Count} records to {outputFileName}");

            // Step 12 Write out the JSON File for User Information

            // First, fetch all the info from the database
            List<UserInformation> userInformation = UserTasks.FetchAllProcessedUsers(db, reportDate, apiInformation: apiInformation);
            // Make a mega object
            multipleJsonLines = new List<string>();
            foreach (var user in userInformation) {
                var stat = new StatsInformationUser();

                stat.UserInformationId = user.UserInformationId;
                stat.UserId = user.UserId;
                stat.Name = user.Name;
                stat.Email = user.Email;
                stat.Mobile = user.Mobile;
                stat.TimeZone = user.TimeZone;
                stat.Language = user.Language;
                stat.State = user.State;
                stat.OwnerId = user.OwnerId;
                stat.OwnerName = user.OwnerName;
                stat.OwnerType = user.OwnerType;
                stat.DefaultClientId = user.DefaultClientId;
                stat.DefaultClientName = user.DefaultClientName;
                stat.CostCentreId = user.CostCentreId;
                stat.CostCentreName = user.CostCentreName;
                stat.UserStatsStatus = user.UserStatsStatus;
                stat.ModifiedDate = user.ModifiedDate;
                stat.CreatedTimestamp = user.CreatedTimestamp;
                stat.TotpType = user.TotpType;
                stat.TotpDate = user.TotpDate;
                stat.TotpVerified = user.TotpVerified;
                // woohoo now we have the mega object.
                // Serialize with options that include null values
                multipleJsonLines.Add(JsonSerializer.Serialize(stat, CreateJsonOptionsWithNulls()));
            }

            // Write the output file
            outputFileName = $"{outputFileName.Replace(".json", "")}_users.json";
            StatsLogger.Log(stats: apiInformation, $"Writing output to {outputFileName}");
            await File.WriteAllLinesAsync(outputFileName, multipleJsonLines);
            StatsLogger.Log(stats: apiInformation, $"Successfully wrote {multipleJsonLines.Count} records to {outputFileName}");
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Unhandled exception: {ex.Message}");
            _logger.LogError(exception: ex, "Unhandled exception in main execution flow");
            return (int)ExitCode.UnknownError;
        }

        StatsLogger.Log(stats: apiInformation, "Stats2fa completed successfully");
        return (int)ExitCode.Success;
    }
}

internal enum ExitCode {
    Success = 0,
    TooEarly = 1,
    UnknownError = 10
}