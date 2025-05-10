using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class Vendor {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("website")] public string? Website { get; set; }
    [JsonPropertyName("timeZoneId")] public string? TimeZoneId { get; set; }
    [JsonPropertyName("language")] public string? Language { get; set; }
    [JsonPropertyName("pin")] public string? Pin { get; set; }
    [JsonPropertyName("group")] public string? Group { get; set; }

    [JsonPropertyName("availableMapSets")] public List<Common.AvailableMapSet>? AvailableMapSets { get; set; }
    [JsonPropertyName("availableDeviceTypes")] public List<object>? AvailableDeviceTypes { get; set; }
    [JsonPropertyName("defaultMapSet")] public Common.AvailableMapSet? DefaultMapSet { get; set; }
    [JsonPropertyName("mapSet")] public Common.MapSet? MapSet { get; set; }

    [JsonPropertyName("owner")] public Common.Owner? owner { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
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
    [JsonPropertyName("measurementUnits")] public Common.MeasurementUnits? MeasurementUnits { get; set; }
    [JsonPropertyName("meta")] public Common.Meta? Meta { get; set; }
}