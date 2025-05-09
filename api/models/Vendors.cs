using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class Vendors {
    [JsonPropertyName("offset")] public int Offset { get; set; }

    [JsonPropertyName("limit")] public int Limit { get; set; }

    [JsonPropertyName("count")] public int Count { get; set; }

    [JsonPropertyName("items")] public List<Vendor> VendorList { get; set; }
}