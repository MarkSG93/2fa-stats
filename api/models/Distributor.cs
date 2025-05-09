using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class Distributor {
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }
    [JsonPropertyName("availableMapSets")] public List<Models.AvailableMapSet> AvailableMapSets { get; set; }
}