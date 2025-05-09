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
    [JsonPropertyName("availableMapSets")] public List<AvailableMapSet>? AvailableMapSets { get; set; }

    [JsonPropertyName("availableEmailProviders")]
    public List<AvailableEmailProvider>? AvailableEmailProviders { get; set; }

    [JsonPropertyName("owner")] public Owner? Owner { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("theme")] public Theme? Theme { get; set; }
    [JsonPropertyName("customFields")] public Dictionary<string, object>? CustomFields { get; set; }
    [JsonPropertyName("domains")] public List<string>? Domains { get; set; }
    [JsonPropertyName("support")] public Support? Support { get; set; }
    [JsonPropertyName("messages")] public Messages? Messages { get; set; }
    [JsonPropertyName("limits")] public Limits? Limits { get; set; }
    [JsonPropertyName("flags")] public Dictionary<string, object>? Flags { get; set; }
    [JsonPropertyName("oidc")] public Dictionary<string, object>? Oidc { get; set; }
    [JsonPropertyName("entity")] public Entity? Entity { get; set; }
    [JsonPropertyName("emailProvider")] public EmailProvider? EmailProvider { get; set; }
    [JsonPropertyName("sslCertificates")] public List<SslCertificate>? SslCertificates { get; set; }
    [JsonPropertyName("retention")] public Retention? Retention { get; set; }
    [JsonPropertyName("passwordPolicy")] public PasswordPolicy? passwordPolicy { get; set; }
    [JsonPropertyName("vendorGroups")] public List<VendorGroup>? VendorGroups { get; set; }
    [JsonPropertyName("features")] public Features? Features { get; set; }
}

public class Owner {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
}

public class Theme {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("settings")] public Dictionary<string, object> Settings { get; set; }
}

public class Support {
    [JsonPropertyName("email")] public string Email { get; set; }
    [JsonPropertyName("phone")] public string Phone { get; set; }
}

public class Messages {
    [JsonPropertyName("login")] public string Login { get; set; }
    [JsonPropertyName("suspended")] public string Suspended { get; set; }
}

public class Limits {
    [JsonPropertyName("entities")] public Entities Entities { get; set; }
}

public class Entities {
    [JsonPropertyName("user")] public EntityLimit User { get; set; }
    [JsonPropertyName("userrole")] public EntityLimit UserRole { get; set; }
    [JsonPropertyName("vendor")] public EntityLimit Vendor { get; set; }
    [JsonPropertyName("dashboard")] public EntityLimit Dashboard { get; set; }

    [JsonPropertyName("dashboardtemplate")]
    public EntityLimit DashboardTemplate { get; set; }

    [JsonPropertyName("emailprovider")] public EntityLimit EmailProvider { get; set; }
    [JsonPropertyName("mapset")] public EntityLimit MapSet { get; set; }
    [JsonPropertyName("themeconfig")] public EntityLimit ThemeConfig { get; set; }
}

public class EntityLimit {
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("active")] public int Active { get; set; }
    [JsonPropertyName("max")] public int Max { get; set; }
}

public class Entity {
    [JsonPropertyName("creationDate")] public string CreationDate { get; set; }
    [JsonPropertyName("modifiedDate")] public string ModifiedDate { get; set; }
}

public class EmailProvider {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }
}

public class SslCertificate {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("domain")] public string Domain { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }
    [JsonPropertyName("modifiedDate")] public string ModifiedDate { get; set; }
}

public class Retention {
    [JsonPropertyName("source")] public Source Source { get; set; }
    [JsonPropertyName("retainFor")] public int RetainFor { get; set; }
    [JsonPropertyName("retainForUnit")] public string RetainForUnit { get; set; }
    [JsonPropertyName("horizonDate")] public string HorizonDate { get; set; }
}

public class Source {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
}

public class PasswordPolicy {
    [JsonPropertyName("source")] public Source Source { get; set; }
    [JsonPropertyName("passwordLength")] public int PasswordLength { get; set; }

    [JsonPropertyName("passwordComplexity")]
    public PasswordComplexity PasswordComplexity { get; set; }

    [JsonPropertyName("passwordExpirationDays")]
    public int PasswordExpirationDays { get; set; }

    [JsonPropertyName("otpSettings")] public OtpSettings OtpSettings { get; set; }
}

public class PasswordComplexity {
    [JsonPropertyName("mixedCase")] public bool MixedCase { get; set; }
    [JsonPropertyName("alphaNumerical")] public bool AlphaNumerical { get; set; }

    [JsonPropertyName("noCommonPasswords")]
    public bool NoCommonPasswords { get; set; }

    [JsonPropertyName("specialCharacters")]
    public bool SpecialCharacters { get; set; }
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
    [JsonPropertyName("tokenValidityDays")]
    public int TokenValidityDays { get; set; }
}

public class VendorGroup {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
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

// Use the AvailableMapSet from Models to maintain compatibility
public class AvailableMapSet : Models.AvailableMapSet {
}

public class AvailableEmailProvider {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }
}