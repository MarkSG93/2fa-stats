using System;
using System.Collections.Generic;
using System.Linq;
using Stats2fa.api;

namespace Stats2fa.utils;

internal static class StringUtils {
    public static string ToPascalCase(this string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (text.Length < 2) return text.ToUpperInvariant();
        if (!text.Contains('_')) return text.ToUpperInvariant();
        return text.Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1)).Aggregate(string.Empty, (s1, s2) => s1 + s2);
    }

    public static string Escape(this string text)
    {
        if (text.Contains('"')) return System.Uri.EscapeDataString(text);
        return text;
    }

    public static string Log(DateTime? time, string? environment, string? distributorId, string? vendorId, string? clientId, string message)
    {
        return Log(time: time, environment, distributorId, vendorId, clientId, new List<string> { message });
    }

    public static string Log(DateTime? time, string? environment, string? distributorId, string? vendorId, string? clientId, ApiInformation stats, string message)
    {
        return Log(time: time, environment, distributorId, vendorId, clientId, new List<string> { $"{stats.ApiCallsDistributors + stats.ApiCallsVendors + stats.ApiCallsClients} calls", message });
    }

    public static string Log(DateTime? time, string? environment, string? distributorId, string? vendorId, string? clientId, List<string> messages)
    {
        var timeStr = $"{DateTime.UtcNow:s}";
        var envStr = "   ";
        var disStr = Guid.Empty.ToString();
        var venStr = Guid.Empty.ToString();
        var cliStr = Guid.Empty.ToString();
        if (time != null) timeStr = $"{time:s}";
        if (!string.IsNullOrEmpty(environment)) envStr = environment;
        if (!string.IsNullOrEmpty(distributorId)) disStr = distributorId;
        if (!string.IsNullOrEmpty(vendorId)) venStr = vendorId;
        if (!string.IsNullOrEmpty(clientId)) cliStr = clientId;
        return $"[{timeStr}],[{envStr}],[{disStr}],[{venStr}],[{cliStr}],{string.Join(',', messages)}";
    }
}