using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Stats2fa.api;

namespace Stats2fa.logger;

public class StatsLogger {
    private static ILogger _logger;

    private static readonly Action<ILogger, int, string, string, string, string, string, Exception?> _logStatsDetails = LoggerMessage.Define<
        int, // api call count
        string, // environment
        string, // distributorId
        string, // vendorId
        string, // clientId
        string // message
    >(LogLevel.Information,
        new EventId(1, "Stats2fa"),
        "ApiCall: {Count} | Environment: {Environment} | Distributor: {DistributorId} | Vendor: {VendorId} | Client: {ClientId} | Message: {Message}");

    public static void InitializeLogger(ILogger logger) {
        _logger = logger;
    }

    public static void Log(ApiInformation? stats, string message, string? distributor, string? vendor, string? client) {
        var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}|ApiCall: {stats.ApiCallsDistributors + stats.ApiCallsVendors + stats.ApiCallsClients} calls | Environment: {stats.Environment} | Distributor: {distributor} | Vendor: {vendor} | Client: {client} | Message: {message}";

        // Log to the logger if available
        if (_logger != null) {
            _logger.LogInformation(logEntry);
        }
        else {
            // If the logger isn't initialized, log to console (for debugging)
            Console.WriteLine(logEntry);
        }
    }
}