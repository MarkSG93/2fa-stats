using System;
using System.Text.Json.Serialization;

namespace Stats2fa.api;

internal class ApiInformation {
    public int ApiInformationId { get; set; }
    [JsonPropertyName("api_date")] public DateTime LastUpdated { get; set; }
    [JsonPropertyName("api_distributors")] public int Distributors { get; set; }
    [JsonPropertyName("api_vendors")] public int Vendors { get; set; }
    [JsonPropertyName("api_clients")] public int Clients { get; set; }
    [JsonPropertyName("api_calls_distributors")] public int ApiCallsDistributors { get; set; }
    [JsonPropertyName("api_calls_vendors")] public int ApiCallsVendors { get; set; }
    [JsonPropertyName("api_calls_clients")] public int ApiCallsClients { get; set; }
}