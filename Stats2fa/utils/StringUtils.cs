using System;
using System.Collections.Generic;
using System.Linq;
using Stats2fa.api;

namespace Stats2fa.utils;

internal static class StringUtils {
    public static string ToPascalCase(this string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (text.Length < 2) return text.ToUpperInvariant();
        if (!text.Contains('_')) return text.ToUpperInvariant();
        return text.Split(new[] { "_" }, options: StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1)).Aggregate(seed: string.Empty, (s1, s2) => s1 + s2);
    }

    public static string Escape(this string text) {
        if (text.Contains('"')) return Uri.EscapeDataString(stringToEscape: text);
        return text;
    }

    public static string Log(DateTime? time, string? environment, string? distributorId, string? vendorId, string? clientId, string message) {
        return Log(time: time,
            environment: environment,
            distributorId: distributorId,
            vendorId: vendorId,
            clientId: clientId,
            new List<string> {
                message
            });
    }

    public static string Log(DateTime? time, string? environment, string? distributorId, string? vendorId, string? clientId, ApiInformation? stats, string message) {
        if (stats == null)
            return Log(time: time,
                environment: environment,
                distributorId: distributorId,
                vendorId: vendorId,
                clientId: clientId,
                new List<string> {
                    message
                });

        return Log(time: time,
            environment: environment,
            distributorId: distributorId,
            vendorId: vendorId,
            clientId: clientId,
            new List<string> {
                $"{stats.ApiCallsDistributors + stats.ApiCallsVendors + stats.ApiCallsClients} calls",
                message
            });
    }

    public static string Log(DateTime? time, string? environment, string? distributorId, string? vendorId, string? clientId, List<string> messages) {
        var timeStr = $"{DateTime.UtcNow:s}";
        var envStr = "   ";
        var disStr = Guid.Empty.ToString();
        var venStr = Guid.Empty.ToString();
        var cliStr = Guid.Empty.ToString();
        if (time != null) timeStr = $"{time:s}";
        if (!string.IsNullOrEmpty(value: environment)) envStr = environment;
        if (!string.IsNullOrEmpty(value: distributorId)) disStr = distributorId;
        if (!string.IsNullOrEmpty(value: vendorId)) venStr = vendorId;
        if (!string.IsNullOrEmpty(value: clientId)) cliStr = clientId;
        return $"[{timeStr}],[{envStr}],[{disStr}],[{venStr}],[{cliStr}],{string.Join(',', values: messages)}";
    }
}