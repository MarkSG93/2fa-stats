using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class Client {
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("website")] public string? Website { get; set; }
    [JsonPropertyName("timeZoneId")] public string? TimeZoneId { get; set; }
    [JsonPropertyName("language")] public string? Language { get; set; }
    [JsonPropertyName("pin")] public string? Pin { get; set; }
    [JsonPropertyName("group")] public string? Group { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }

    // Entity info from separate field in the API
    [JsonPropertyName("creationDate")] public string? CreationDate { get; set; }
    [JsonPropertyName("modifiedDate")] public string? ModifiedDate { get; set; }

    [JsonPropertyName("counts")] public Counts? Count { get; set; }
    [JsonPropertyName("owner")] public Common.Owner? Owner { get; set; }
    [JsonPropertyName("mapSet")] public Common.MapSet? MapSet { get; set; }

    [JsonPropertyName("theme")] public Common.Theme? Theme { get; set; }
    [JsonPropertyName("customFields")] public Dictionary<string, List<Common.CustomField>>? CustomFields { get; set; }
    [JsonPropertyName("domains")] public List<string>? Domains { get; set; }
    [JsonPropertyName("address")] public Common.Address? Address { get; set; }
    [JsonPropertyName("support")] public Common.Support? Support { get; set; }
    [JsonPropertyName("messages")] public Common.Messages? Messages { get; set; }
    [JsonPropertyName("limits")] public Common.Limits? Limits { get; set; }
    [JsonPropertyName("flags")] public Dictionary<string, object>? Flags { get; set; }
    [JsonPropertyName("oidc")] public Dictionary<string, object>? Oidc { get; set; }
    [JsonPropertyName("entity")] public Common.Entity? Entity { get; set; }
    [JsonPropertyName("emailProvider")] public Common.EmailProvider? EmailProvider { get; set; }
    [JsonPropertyName("sslCertificates")] public List<Common.SslCertificate>? SslCertificates { get; set; }
    [JsonPropertyName("retention")] public Common.Retention? Retention { get; set; }
    [JsonPropertyName("passwordPolicy")] public Common.PasswordPolicy? passwordPolicy { get; set; }
    [JsonPropertyName("features")] public Common.Features? Features { get; set; }

    [JsonPropertyName("availableDeviceTypes")] public List<object>? AvailableDeviceTypes { get; set; }

    [JsonPropertyName("measurementUnits")] public Common.MeasurementUnits? MeasurementUnits { get; set; }
    [JsonPropertyName("meta")] public Common.Meta? Meta { get; set; }

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

    public bool Equals(Client x, Client y) {
        return x.Id.Equals(y.Id);
    }

    public int GetHashCode(Client obj) {
        return obj.Id.GetHashCode();
    }
}