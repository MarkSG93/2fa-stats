using System;
using System.Text.Json.Serialization;
using Stats2fa.api.models;

namespace Stats2fa.database;

public class ClientInformation {
    public long ClientInformationId { get; set; }

    // Non nullable info
    [JsonPropertyName("client_date")]
    public DateTime CreatedTimestamp { get; set; }

    [JsonPropertyName("client_vendor_id")]
    public string ClientVendorId { get; set; }

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    [JsonPropertyName("client_name")]
    public string ClientName { get; set; }

    [JsonPropertyName("client_type")]
    public string ClientType { get; set; }

    // Nullable info
    [JsonPropertyName("client_stats_status")]
    public string? ClientStatsStatus { get; set; }

    [JsonPropertyName("client_status")]
    public string? ClientStatus { get; set; }

    [JsonPropertyName("client_passwordPolicy_source_id")]
    public string? ClientPasswordPolicySourceId { get; set; }

    [JsonPropertyName("client_passwordPolicy_source_name")]
    public string? ClientPasswordPolicySourceName { get; set; }

    [JsonPropertyName("client_passwordPolicy_source_type")]
    public string? ClientPasswordPolicySourceType { get; set; }

    [JsonPropertyName("client_passwordPolicy_passwordLength")]
    public int? ClientPasswordPolicyPasswordLength { get; set; }

    [JsonPropertyName("client_passwordPolicy_passwordComplexity_mixedCase")]
    public bool? ClientPasswordPolicyPasswordComplexityMixedcase { get; set; }

    [JsonPropertyName("client_passwordPolicy_passwordComplexity_alphaNumerical")]
    public bool? ClientPasswordPolicyPasswordComplexityAlphanumerical { get; set; }

    [JsonPropertyName("client_passwordPolicy_passwordComplexity_noCommonPasswords")]
    public bool? ClientPasswordPolicyPasswordComplexityNocommonpasswords { get; set; }

    [JsonPropertyName("client_passwordPolicy_passwordComplexity_specialCharacters")]
    public bool? ClientPasswordPolicyPasswordComplexitySpecialcharacters { get; set; }

    [JsonPropertyName("client_passwordPolicy_passwordExpirationDays")]
    public int? ClientPasswordPolicyPasswordExpirationDays { get; set; }

    [JsonPropertyName("client_passwordPolicy_otpSettings_methods_totp_tokenValidityDays")]
    public int? ClientPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays { get; set; }

    [JsonPropertyName("client_passwordPolicy_otpSettings_methods_email_tokenValidityDays")]
    public int? ClientPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays { get; set; }

    [JsonPropertyName("client_passwordPolicy_otpSettings_gracePeriodDays")]
    public int? ClientPasswordPolicyOtpSettingsGracePeriodDays { get; set; }

    [JsonPropertyName("client_passwordPolicy_otpSettings_mandatoryFor")]
    public string? ClientPasswordPolicyOtpSettingsMandatoryFor { get; set; }

    [JsonPropertyName("client_users")]
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Users? ClientUsers { get; set; }

    public object this[string propertyName] {
        get {
            var myType = typeof(ClientInformation);
            var myPropInfo = myType.GetProperty(name: propertyName);
            return myPropInfo.GetValue(this, null);
        }
        set {
            var myType = typeof(ClientInformation);
            var myPropInfo = myType.GetProperty(name: propertyName);
            myPropInfo.SetValue(this, value: value, null);
        }
    }
}