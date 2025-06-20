using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Stats2fa.api;

namespace Stats2fa.logger;

public class StatsLogger {
    private static ILogger _logger;
    //
    // private static readonly Action<ILogger, int, string, string, string, string, string, Exception?> _logStatsDetails = LoggerMessage.Define<
    //     int, // api call count
    //     string, // environment
    //     string, // distributorId
    //     string, // vendorId
    //     string, // clientId
    //     string // message
    // >(LogLevel.Information,
    //     new EventId(1, "Stats2fa"),
    //     $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} | ApiCall: {{Count}} | Environment: {{Environment}} | Distributor: {{DistributorId}} | Vendor: {{VendorId}} | Client: {{ClientId}} | Message: {{Message}}");

    //
    // private static readonly Action<ILogger, string, Exception?> _logStatsDetails = LoggerMessage.Define<
    //     string // message
    // >(LogLevel.Information,
    //     new EventId(1, "Stats2fa"),
    //     $"{{Message}}");
    //

    public static void InitializeLogger(ILogger logger) {
        _logger = logger;
    }

    public static void Log(ApiInformation? stats, string message, string? environment = null, string? distributor = null, string? vendor = null, string? client = null) {
        var logEntry = new StringBuilder($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} | ");

        // Handle null ApiInformation
        if (stats != null) {
            var totalCalls = stats.ApiCallsDistributors + stats.ApiCallsVendors + stats.ApiCallsClients;
            logEntry.Append($"ApiCall: {totalCalls:000000} calls | ");
        }
        else {
            logEntry.Append($"ApiCall: {0:000000} calls | ");
        }

        // Environment can come from stats or as a parameter
        var envValue = "";
        if (stats != null && stats.Environment != null) {
            envValue = stats.Environment;
        }
        else {
            if (environment != null)
                envValue = environment;
            else
                envValue = "unknown";
        }

        logEntry.Append($"Environment: {envValue} | ");

        // Add the remaining fields
        logEntry.Append($"Distributor: {distributor ?? "null"} | ");
        logEntry.Append($"Vendor: {vendor ?? "null"} | ");
        logEntry.Append($"Client: {client ?? "null"} | ");
        logEntry.Append($"Message: {message}");

        // Log to the logger if available

        if (_logger != null)
            _logger.LogInformation(logEntry.ToString());
        // log to structured logger if defined
        // if (stats != null) {
        //     _logStatsDetails(_logger, message, null);
        // }
        // else {
        //     _logger.LogInformation(logEntry.ToString());
        // }
        else
            // If the logger isn't initialized, log to console (for debugging)
            Console.WriteLine(logEntry.ToString());
    }
}