using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class Common {
    public class Source {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
    }

    public class PasswordPolicy {
        [JsonPropertyName("source")] public Source? Source { get; set; }
        [JsonPropertyName("passwordLength")] public int PasswordLength { get; set; }
        [JsonPropertyName("passwordComplexity")] public PasswordComplexity? PasswordComplexity { get; set; }
        [JsonPropertyName("passwordExpirationDays")] public int PasswordExpirationDays { get; set; }
        [JsonPropertyName("otpSettings")] public OtpSettings? OtpSettings { get; set; }
    }
    
    public class Owner {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    public class PasswordComplexity {
        [JsonPropertyName("mixedCase")] public bool MixedCase { get; set; }
        [JsonPropertyName("alphaNumerical")] public bool AlphaNumerical { get; set; }
        [JsonPropertyName("noCommonPasswords")] public bool NoCommonPasswords { get; set; }
        [JsonPropertyName("specialCharacters")] public bool SpecialCharacters { get; set; }
    }

    public class OtpSettings {
        [JsonPropertyName("methods")] public OtpMethods Methods { get; set; }
        [JsonPropertyName("gracePeriodDays")] public int GracePeriodDays { get; set; }
        [JsonPropertyName("mandatoryFor")] public string MandatoryFor { get; set; }
    }

    public class OtpMethods {
        [JsonPropertyName("totp")] public TokenValidity Totp { get; set; }
        [JsonPropertyName("email")] public TokenValidity Email { get; set; }
    }

    public class TokenValidity {
        [JsonPropertyName("tokenValidityDays")] public int TokenValidityDays { get; set; }
    }

    public class Features {
        [JsonPropertyName("analytics")] public FeatureConfig Analytics { get; set; }
        [JsonPropertyName("roadSpeed")] public FeatureConfig RoadSpeed { get; set; }
        [JsonPropertyName("providerFeatures")] public List<object> ProviderFeatures { get; set; }
    }

    public class FeatureConfig {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; }
        [JsonPropertyName("parameters")] public Dictionary<string, object> Parameters { get; set; }
    }

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