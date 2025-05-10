using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Stats2fa.database;

public class VendorInformation {
    public Int64 VendorInformationId { get; set; }

    // Non nullable info
    [JsonPropertyName("vendor_date")] public DateTime CreatedTimestamp { get; set; }

    [JsonPropertyName("vendor_distributor_id")]
    public string VendorDistributorId { get; set; }

    [JsonPropertyName("vendor_id")] public string VendorId { get; set; }
    [JsonPropertyName("vendor_name")] public string VendorName { get; set; }
    [JsonPropertyName("vendor_type")] public string VendorType { get; set; }
    [JsonPropertyName("vendor_status")] public string VendorStatus { get; set; }

    // Nullable info
// Nullable info
    [JsonPropertyName("vendor_last_updated")]
    public DateTime? LastUpdatedTimestamp { get; set; }

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
    public object this[string propertyName]
    {
        get
        {
            Type myType = typeof(VendorInformation);
            PropertyInfo myPropInfo = myType.GetProperty(propertyName);
            return myPropInfo.GetValue(this, null);
        }
        set
        {
            Type myType = typeof(VendorInformation);
            PropertyInfo myPropInfo = myType.GetProperty(propertyName);
            myPropInfo.SetValue(this, value, null);
        }
    }
}