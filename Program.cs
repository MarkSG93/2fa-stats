using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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

namespace Stats2fa {
    class Program {
        private static ILogger _logger;

        static async Task<int> Main(string[] args) {
            // Create the logger factory and logger
            using var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole().SetMinimumLevel(LogLevel.Information); });
            _logger = loggerFactory.CreateLogger<Program>(); // Assign logger to static field

            // Initialize PacketLogger with the logger
            _logger.LogInformation($"Stats2fa starting");
            StatsLogger.InitializeLogger(_logger);

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var environment = "eu1";
            var reportDate = DateTime.UtcNow.Date;
            String apiKey = null, baseAddress = null, dbFileName = null, outputFileName = null;

            if (!string.IsNullOrEmpty(config["env"])) environment = config["env"];
            StatsLogger.Log(null, $"arguments supplied date={config["date"]}");

            if (string.IsNullOrEmpty(config["date"])) {
                StatsLogger.Log(null, $"Warning: No date specified in arguments. Using current UTC date: {reportDate:yyyy-MM-dd}", environment: environment);
            }
            else {
                reportDate = DateTime.ParseExact(config["date"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                StatsLogger.Log(null, $"Date specified: {reportDate:yyyy-MM-dd}", environment: environment);
            }

            switch (environment) {
                case "eu1":
                    apiKey = config["API_EU1"]!;
                    baseAddress = config["ApiStrings:eu1"]!;
                    dbFileName = config["DatabaseConfiguration:eu1"]!;
                    outputFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{config["DatabaseConfiguration:eu1"]!}_{DateTime.UtcNow:yyyy-M-d-hh-mm-ss}.json");
                    break;
                case "us1":
                    apiKey = config["API_US1"]!;
                    baseAddress = config["ApiStrings:us1"]!;
                    dbFileName = config["DatabaseConfiguration:us1"]!;
                    outputFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{config["DatabaseConfiguration:us1"]!}_{DateTime.UtcNow:yyyy-M-d-hh-mm-ss}.json");
                    break;
                case "uk1":
                    apiKey = config["API_UK1"]!;
                    baseAddress = config["ApiStrings:uk1"]!;
                    dbFileName = config["DatabaseConfiguration:uk1"]!;
                    outputFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{config["DatabaseConfiguration:uk1"]!}_{DateTime.UtcNow:yyyy-M-d-hh-mm-ss}.json");
                    break;
                case "staging":
                    apiKey = config["API_STAGING"]!;
                    baseAddress = config["ApiStrings:staging"]!;
                    dbFileName = config["DatabaseConfiguration:staging"]!;
                    outputFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{config["DatabaseConfiguration:staging"]!}_{DateTime.UtcNow:yyyy-M-d-hh-mm-ss}.json");
                    break;
            }

            //  On macOS, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) typically returns the user's home directory (/Users/username) rather than the Documents folder.
            // This behavior is because .NET on macOS maps SpecialFolder.MyDocuments to the user's home directory, not a "Documents" subdirectory.
            // This is a platform-specific implementation detail of .NET Core on macOS.
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{dbFileName}.db");

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
            if (!string.IsNullOrEmpty(config["deleteDatabase"]) && config["deleteDatabase"].Equals("true") && File.Exists(dbPath)) {
                StatsLogger.Log(apiInformation, $"Deleting existing database at {dbPath}");
                File.Delete(dbPath);
            }

            await using var db = new StatsContext(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{dbFileName}.db");

            // Ensure database is created with current schema
            StatsLogger.Log(apiInformation, $"Ensuring database is created");
            await db.Database.EnsureCreatedAsync();

            if (reportDate > DateTime.UtcNow) {
                StatsLogger.Log(apiInformation, $"Deferring Execution Requested Date {reportDate} is still {(reportDate - DateTime.UtcNow).TotalHours:0000.00} hours in the future.");
                return (int)ExitCode.TooEarly;
            }


            // Step 0 Setup HttpClient
            StatsLogger.Log(apiInformation, "Setting up HTTP client");
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

            var rateLimiter = new SlidingWindowRateLimiter(slidingWindowRateLimiterOptions);

            using HttpClient httpClient = new HttpClient(new RetryHandler(new ClientSideRateLimitedHandler(limiter: rateLimiter)));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            httpClient.Timeout = TimeSpan.FromSeconds(240); // fml vendor has 8004 clients
            httpClient.BaseAddress = new Uri(baseAddress);

            try {
                // Step 1 Fetch the Distributors
                StatsLogger.Log(apiInformation, "Fetching distributors");
                Distributors distributors = await ApiUtils.GetDistributors(httpClient, apiInformation, null, Convert.ToInt32(config["ApiQueryLimits:DistributorLimit"]), Convert.ToInt32(config["ApiQueryLimits:DistributorMax"]));
                StatsLogger.Log(apiInformation, $"Total distributors ({distributors.Count:00000})");
                await Cache.SaveDistributors(db, distributors, reportDate);

                if (distributors.DistributorList.Count == 0) {
                    StatsLogger.Log(apiInformation, "No distributors found. Exiting.");
                    return (int)ExitCode.Success;
                }

                // Step 2 Fetch the Vendors
                StatsLogger.Log(apiInformation, "Fetching vendors");
                ConcurrentBag<Vendor> allVendors = new ConcurrentBag<Vendor>();
                try {
                    await Task.WhenAll(Parallel.ForEachAsync(source: distributors.DistributorList, (distributor, cancellationToken) =>
                        ApiUtils.GetVendorsForDistributor(allVendors, httpClient, apiInformation, distributor, null, cancellationToken,
                            Convert.ToInt32(config["ApiQueryLimits:VendorLimit"]), Convert.ToInt32(config["ApiQueryLimits:VendorMax"]))));
                }
                catch (Exception ex) {
                    StatsLogger.Log(apiInformation, $"Error during vendor fetching: {ex.Message}");
                    // Continue with the vendors we were able to fetch
                }

                StatsLogger.Log(apiInformation, $"Total vendors ({allVendors.Count:00000})");
                await Cache.SaveVendors(db, allVendors, reportDate);

                if (allVendors.Count == 0) {
                    StatsLogger.Log(apiInformation, "No vendors found. Exiting.");
                    return (int)ExitCode.Success;
                }

                // Step 3 Fetch the Clients
                StatsLogger.Log(apiInformation, "Fetching clients");
                ConcurrentBag<Client> allClients = new ConcurrentBag<Client>();
                try {
                    await Task.WhenAll(Parallel.ForEachAsync(source: allVendors, (vendor, cancellationToken) =>
                        ApiUtils.GetClientsForVendor(allClients, httpClient, apiInformation, vendor, null, cancellationToken,
                            Convert.ToInt32(config["ApiQueryLimits:ClientLimit"]), Convert.ToInt32(config["ApiQueryLimits:ClientMax"]))));
                }
                catch (Exception ex) {
                    StatsLogger.Log(apiInformation, $"Error during client fetching: {ex.Message}");
                    // Continue with the clients we were able to fetch
                }

                StatsLogger.Log(apiInformation, $"Total clients ({allClients.Count:00000})");
                await Cache.SaveClients(db, allClients, reportDate);

                // Step 4 Fetch the Distributor Information
                StatsLogger.Log(apiInformation, "Fetching distributor information");
                await DistributorTasks.PopulateDistributorInformation(httpClient, apiInformation, db, reportDate);

                // Step 5 Fetch the Vendor Information
                StatsLogger.Log(apiInformation, "Fetching vendor information");
                await VendorTasks.PopulateVendorInformation(httpClient, apiInformation, db, reportDate);

                // Step 6 Fetch the Client Information
                StatsLogger.Log(apiInformation, "Fetching client information");
                await ClientTasks.PopulateClientInformation(httpClient, apiInformation, db, reportDate, Convert.ToInt32(config["ApiQueryLimits:ClientMax"]));
            }
            catch (Exception ex) {
                StatsLogger.Log(apiInformation, $"Unhandled exception: {ex.Message}");
                _logger.LogError(ex, "Unhandled exception in main execution flow");
                return (int)ExitCode.UnknownError;
            }

            StatsLogger.Log(apiInformation, "Stats2fa completed successfully");
            return (int)ExitCode.Success;
        }
    }

    internal enum ExitCode : int {
        Success = 0,
        TooEarly = 1,
        UnknownError = 10
    }
}