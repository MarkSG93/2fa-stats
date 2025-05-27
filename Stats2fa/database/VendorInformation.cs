using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Stats2fa.api.models;

namespace Stats2fa.database;

public class VendorInformation {
    public long VendorInformationId { get; set; }

    // Non nullable info
    [JsonPropertyName("vendor_date")]
    public DateTime CreatedTimestamp { get; set; }

    [JsonPropertyName("vendor_distributor_id")]
    public string VendorDistributorId { get; set; }

    [JsonPropertyName("vendor_id")]
    public string VendorId { get; set; }

    [JsonPropertyName("vendor_name")]
    public string VendorName { get; set; }

    [JsonPropertyName("vendor_type")]
    public string VendorType { get; set; }

    [JsonPropertyName("vendor_status")]
    public string VendorStatus { get; set; }

    // Nullable info
    [JsonPropertyName("vendor_passwordPolicy_source_id")]
    public string? VendorPasswordPolicySourceId { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_source_name")]
    public string? VendorPasswordPolicySourceName { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_source_type")]
    public string? VendorPasswordPolicySourceType { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_passwordLength")]
    public int? VendorPasswordPolicyPasswordLength { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_passwordComplexity_mixedCase")]
    public bool? VendorPasswordPolicyPasswordComplexityMixedcase { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_passwordComplexity_alphaNumerical")]
    public bool? VendorPasswordPolicyPasswordComplexityAlphanumerical { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_passwordComplexity_noCommonPasswords")]
    public bool? VendorPasswordPolicyPasswordComplexityNocommonpasswords { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_passwordComplexity_specialCharacters")]
    public bool? VendorPasswordPolicyPasswordComplexitySpecialcharacters { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_passwordExpirationDays")]
    public int? VendorPasswordPolicyPasswordExpirationDays { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_otpSettings_methods_totp_tokenValidityDays")]
    public int? VendorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_otpSettings_methods_email_tokenValidityDays")]
    public int? VendorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_otpSettings_gracePeriodDays")]
    public int? VendorPasswordPolicyOtpSettingsGracePeriodDays { get; set; }

    [JsonPropertyName("vendor_passwordPolicy_otpSettings_mandatoryFor")]
    public string? VendorPasswordPolicyOtpSettingsMandatoryFor { get; set; }
    
    [JsonPropertyName("vendor_stats_status")]
    public string? VendorStatsStatus { get; set; }
    
    public object this[string propertyName] {
        get {
            var myType = typeof(VendorInformation);
            var myPropInfo = myType.GetProperty(name: propertyName);
            return myPropInfo.GetValue(this, null);
        }
        set {
            var myType = typeof(VendorInformation);
            var myPropInfo = myType.GetProperty(name: propertyName);
            myPropInfo.SetValue(this, value: value, null);
        }
    }
}