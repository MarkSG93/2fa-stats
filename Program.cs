using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Stats2fa.api;
using Stats2fa.api.models;
using Stats2fa.database;
using Stats2fa.utils;

namespace Stats2fa {
    class Program {
        static async Task<int> Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var environment = "eu1";
            var reportDate = DateTime.UtcNow.Date;
            String apiKey = null, baseAddress = null, dbFileName = null, outputFileName = null;

            if (!string.IsNullOrEmpty(config["env"])) environment = config["env"];
            Console.WriteLine($"date={config["date"]}");
            if (string.IsNullOrEmpty(config["date"])) {
                Console.WriteLine("Warning: No date specified in arguments. Using current UTC date: " + reportDate.ToString("yyyy-MM-dd"));
            }
            else {
                reportDate = DateTime.ParseExact(config["date"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
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
            }

            await using var db = new StatsContext(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{dbFileName}.db");

            if (reportDate > DateTime.UtcNow) {
                Console.WriteLine($"Deferring Execution Requested Date {reportDate} is still {(reportDate - DateTime.UtcNow).TotalHours:0000.00} hours in the future.");
                return (int)ExitCode.TooEarly;
            }

            var apiInformation = new ApiInformation {
                LastUpdated = DateTime.UtcNow,
                Distributors = 0,
                Vendors = 0,
                Clients = 0,
                ApiCallsDistributors = 0,
                ApiCallsVendors = 0,
                ApiCallsClients = 0
            };
            
            // Step 0 Setup HttpClient
            var slidingWindowRateLimiterOptions = new SlidingWindowRateLimiterOptions {
                Window = TimeSpan.FromSeconds(1),
                SegmentsPerWindow = 1,
                AutoReplenishment = true,
                PermitLimit = 4, // requests per window
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10000
            };

            var rateLimiter = new SlidingWindowRateLimiter(slidingWindowRateLimiterOptions);

            using HttpClient httpClient = new HttpClient(new RetryHandler(new ClientSideRateLimitedHandler(limiter: rateLimiter)));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            httpClient.Timeout = TimeSpan.FromSeconds(240); // fml vendor has 8004 clients
            httpClient.BaseAddress = new Uri(baseAddress);

            // Step 1 Fetch the Distributors
            Distributors distributors = await ApiUtils.GetDistributors(httpClient, apiInformation, null, Convert.ToInt32(config["ApiQueryLimits:DistributorLimit"]), Convert.ToInt32(config["ApiQueryLimits:DistributorMax"]));
            Console.WriteLine("\n" + StringUtils.Log(DateTime.UtcNow, environment, null, null, null, apiInformation, $"total distributors ({distributors.Count:00000})"));
            // await Cache.SaveDistributors(db, distributors, reportDate);

            
            Console.WriteLine("Stats2fa");
            return (int)ExitCode.Success;
        }
    }

    internal enum ExitCode : int {
        Success = 0,
        TooEarly = 1,
        UnknownError = 10
    }
}