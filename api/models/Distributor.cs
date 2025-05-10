using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class Distributor {
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("website")] public string? Website { get; set; }
    [JsonPropertyName("availableMapSets")] public List<Common.AvailableMapSet>? AvailableMapSets { get; set; }

    [JsonPropertyName("availableEmailProviders")]
    public List<AvailableEmailProvider>? AvailableEmailProviders { get; set; }

    [JsonPropertyName("owner")] public Common.Owner? Owner { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("theme")] public Common.Theme? Theme { get; set; }
    [JsonPropertyName("customFields")] public Dictionary<string, object>? CustomFields { get; set; }
    [JsonPropertyName("domains")] public List<string>? Domains { get; set; }
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
    [JsonPropertyName("vendorGroups")] public List<VendorGroup>? VendorGroups { get; set; }
    [JsonPropertyName("features")] public Common.Features? Features { get; set; }
}

public class VendorGroup {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
}

// Use the AvailableMapSet from Common.cs to maintain compatibility

public class AvailableEmailProvider {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }
}