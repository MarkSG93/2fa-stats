using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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

            // Step 11 Write out the JSON File

            // First, fetch all the info from the database
            List<UserInformation> userInformation = UserTasks.FetchAllProcessedUsers(db, reportDate, apiInformation: apiInformation);
            List<ClientInformation> clientInformation = ClientTasks.FetchAllProcessedClients(db, reportDate, apiInformation: apiInformation);
            List<VendorInformation> vendorInformation = VendorTasks.FetchAllProcessedVendors(db, reportDate, apiInformation: apiInformation);
            List<DistributorInformation> distributorInformation = DistributorTasks.FetchAllProcessedDistributors(db, reportDate, apiInformation: apiInformation);

            // Make a mega object
            List<StatsInformation> stats = new List<StatsInformation>();
            var multipleJsonLines = new List<string>();
            foreach (var client in clientInformation) {
                var vendor = vendorInformation.Single(x => x.VendorId == client.ClientVendorId);
                var distributor = distributorInformation.Single(x => x.DistributorId == vendor.VendorDistributorId);
                var stat = new StatsInformation();

                stat.UserCreatedTimestamp = client.CreatedTimestamp;
                stat.ClientId = string.Empty; // TODO ;
                stat.ClientName = string.Empty; // TODO ;
                stat.ClientType = string.Empty; // TODO ;
                stat.ClientStatus = string.Empty; // TODO ;
                stat.ClientVendorId = string.Empty; // TODO ;
                stat.ClientStatsStatus = string.Empty; // TODO ;
                stat.ClientPasswordPolicySourceId = string.Empty; // TODO ;
                stat.ClientPasswordPolicySourceName = string.Empty; // TODO ;
                stat.ClientPasswordPolicySourceType = string.Empty; // TODO ;
                stat.ClientPasswordPolicyPasswordLength = 0; // TODO ;
                stat.ClientPasswordPolicyPasswordComplexityMixedcase = true; // TODO ;
                stat.ClientPasswordPolicyPasswordComplexityAlphanumerical = true; // TODO ;
                stat.ClientPasswordPolicyPasswordComplexityNocommonpasswords = true; // TODO ;
                stat.ClientPasswordPolicyPasswordComplexitySpecialcharacters = true; // TODO ;
                stat.ClientPasswordPolicyPasswordExpirationDays = 0; // TODO ;
                stat.ClientPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = 0; // TODO ;
                stat.ClientPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = 0; // TODO ;
                stat.ClientPasswordPolicyOtpSettingsGracePeriodDays = 0; // TODO ;
                stat.ClientPasswordPolicyOtpSettingsMandatoryFor = string.Empty; // TODO ;

                stat.VendorCreatedTimestamp = DateTime.MinValue; // TODO ;
                stat.VendorDistributorId = string.Empty; // TODO ;
                stat.VendorId = string.Empty; // TODO ;
                stat.VendorName = string.Empty; // TODO ;
                stat.VendorType = string.Empty; // TODO ;
                stat.VendorStatus = string.Empty; // TODO ;
                stat.VendorPasswordPolicySourceId = string.Empty; // TODO ;
                stat.VendorPasswordPolicySourceName = string.Empty; // TODO ;
                stat.VendorPasswordPolicySourceType = string.Empty; // TODO ;
                stat.VendorPasswordPolicyPasswordLength = 0; // TODO ;
                stat.VendorPasswordPolicyPasswordComplexityMixedcase = true; // TODO ;
                stat.VendorPasswordPolicyPasswordComplexityAlphanumerical = true; // TODO ;
                stat.VendorPasswordPolicyPasswordComplexityNocommonpasswords = true; // TODO ;
                stat.VendorPasswordPolicyPasswordComplexitySpecialcharacters = true; // TODO ;
                stat.VendorPasswordPolicyPasswordExpirationDays = 0; // TODO ;
                stat.VendorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = 0; // TODO ;
                stat.VendorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = 0; // TODO ;
                stat.VendorPasswordPolicyOtpSettingsGracePeriodDays = 0; // TODO ;

                stat.DistributorCreatedTimestamp = DateTime.MinValue; // TODO ;
                stat.DistributorId = string.Empty; // TODO ;
                stat.DistributorName = string.Empty; // TODO ;
                stat.DistributorType = string.Empty; // TODO ;
                stat.DistributorStatus = string.Empty; // TODO ;
                stat.DistributorPasswordPolicySourceId = string.Empty; // TODO ;
                stat.DistributorPasswordPolicySourceName = string.Empty; // TODO ;
                stat.DistributorPasswordPolicySourceType = string.Empty; // TODO ;
                stat.DistributorPasswordPolicyPasswordLength = 0; // TODO ;
                stat.DistributorPasswordPolicyPasswordComplexityMixedcase = true; // TODO ;
                stat.DistributorPasswordPolicyPasswordComplexityAlphanumerical = true; // TODO ;
                stat.DistributorPasswordPolicyPasswordComplexityNocommonpasswords = true; // TODO ;
                stat.DistributorPasswordPolicyPasswordComplexitySpecialcharacters = true; // TODO ;
                stat.DistributorPasswordPolicyPasswordExpirationDays = 0; // TODO ;
                stat.DistributorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = 0; // TODO ;
                stat.DistributorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = 0; // TODO ;
                stat.DistributorPasswordPolicyOtpSettingsGracePeriodDays = 0; // TODO ;
                stat.DistributorPasswordPolicyOtpSettingsMandatoryFor = string.Empty; // TODO ;
                stat.DistributorStatsStatus = string.Empty; // TODO ;
                
                // woohoo now we have the mega object.
                multipleJsonLines.Add(JsonSerializer.Serialize(stat));
            }
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