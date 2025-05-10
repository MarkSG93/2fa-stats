using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class Client {
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("website")] public string Website { get; set; }

    [JsonPropertyName("owner")] public Common.Owner Owner { get; set; }

    [JsonPropertyName("group")] public string Group { get; set; }

    [JsonPropertyName("state")] public string State { get; set; }

    [JsonPropertyName("creationDate")] public string CreationDate { get; set; }

    [JsonPropertyName("modifiedDate")] public string ModifiedDate { get; set; }

    [JsonPropertyName("counts")] public Counts Count { get; set; }

    [JsonPropertyName("mapset")] public Common.AvailableMapSet Mapset { get; set; }


    public class Counts {
        [JsonPropertyName("asset")] public int Asset { get; set; }

        [JsonPropertyName("device")] public int Device { get; set; }

        [JsonPropertyName("user")] public int User { get; set; }

        [JsonPropertyName("simcard")] public int Simcard { get; set; }

        [JsonPropertyName("companion-camera")] public int CompanionCamera { get; set; }
    }
}

class ClientComparer : IEqualityComparer<Client> {
    public static readonly ClientComparer Instance = new ClientComparer();

    public bool Equals(Client x, Client y)
    {
        return x.Id.Equals(y.Id);
    }

    public int GetHashCode(Client obj)
    {
        return obj.Id.GetHashCode();
    }
}