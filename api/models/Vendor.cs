using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class Vendor {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }

    [JsonPropertyName("availableMapSets")] public List<Models.AvailableMapSet> AvailableMapSets { get; set; }

    [JsonPropertyName("defaultMapSet")] public Models.AvailableMapSet DefaultMapSet { get; set; }

    [JsonPropertyName("owner")] public Owner owner { get; set; }
}