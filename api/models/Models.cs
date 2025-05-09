using System.Collections.Generic;
using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
#pragma warning disable CS8618

namespace Stats2fa.api.models;

public class Models {
    public class AvailableMapSet {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    public class StatsFilter {
        [JsonPropertyName("groupLevel")] public string groupLevel { get; set; }
        [JsonPropertyName("rowLevel")] public string rowLevel { get; set; }
        [JsonPropertyName("flip")] public bool flip { get; set; }
        [JsonPropertyName("time")] public string time { get; set; }
    }

    public class Owner {
        [JsonPropertyName("id")] public string id { get; set; }

        [JsonPropertyName("name")] public string name { get; set; }
    }

    public class MapLayers {
        [JsonPropertyName("items")] public List<Item> Items { get; set; }

        public class Item {
            [JsonPropertyName("id")] public string Id { get; set; }
            [JsonPropertyName("name")] public string Name { get; set; }
        }
    }

    public class Stats {
        public class Average {
            [JsonPropertyName("value")] public List<string> Value { get; set; }
            [JsonPropertyName("raw")] public List<double> Raw { get; set; }
        }

        public class Cellset {
            [JsonPropertyName("status")] public string Status { get; set; }
            [JsonPropertyName("average")] public Average Average { get; set; }
        }

        [JsonPropertyName("cellset")] public Cellset CellSet { get; set; }
    }
}