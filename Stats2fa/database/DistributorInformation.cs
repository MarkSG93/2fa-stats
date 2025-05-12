using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Stats2fa.api.models;

namespace Stats2fa.database;

public class DistributorInformation {
    public long DistributorInformationId { get; set; }

    // Non nullable info
    [JsonPropertyName("distributor_date")]
    public DateTime CreatedTimestamp { get; set; }

    [JsonPropertyName("distributor_id")]
    public string DistributorId { get; set; }

    [JsonPropertyName("distributor_name")]
    public string DistributorName { get; set; }

    [JsonPropertyName("distributor_type")]
    public string DistributorType { get; set; }

    // Nullable info
    [JsonPropertyName("distributor_status")]
    public string? DistributorStatus { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_source_id")]
    public string? DistributorPasswordPolicySourceId { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_source_name")]
    public string? DistributorPasswordPolicySourceName { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_source_type")]
    public string? DistributorPasswordPolicySourceType { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_passwordLength")]
    public int? DistributorPasswordPolicyPasswordLength { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_passwordComplexity_mixedCase")]
    public bool? DistributorPasswordPolicyPasswordComplexityMixedcase { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_passwordComplexity_alphaNumerical")]
    public bool? DistributorPasswordPolicyPasswordComplexityAlphanumerical { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_passwordComplexity_noCommonPasswords")]
    public bool? DistributorPasswordPolicyPasswordComplexityNocommonpasswords { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_passwordComplexity_specialCharacters")]
    public bool? DistributorPasswordPolicyPasswordComplexitySpecialcharacters { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_passwordExpirationDays")]
    public int? DistributorPasswordPolicyPasswordExpirationDays { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_otpSettings_methods_totp_tokenValidityDays")]
    public int? DistributorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_otpSettings_methods_email_tokenValidityDays")]
    public int? DistributorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_otpSettings_gracePeriodDays")]
    public int? DistributorPasswordPolicyOtpSettingsGracePeriodDays { get; set; }

    [JsonPropertyName("distributor_passwordPolicy_otpSettings_mandatoryFor")]
    public string? DistributorPasswordPolicyOtpSettingsMandatoryFor { get; set; }

    [JsonPropertyName("distributor_users")]
    [NotMapped]
    public Users? DistributorUsers { get; set; }

    [JsonPropertyName("distributor_stats_status")]
    public string? DistributorStatsStatus { get; set; }

    public object this[string propertyName] {
        get {
            var myType = typeof(DistributorInformation);
            var myPropInfo = myType.GetProperty(name: propertyName);
            return myPropInfo.GetValue(this, null);
        }
        set {
            var myType = typeof(DistributorInformation);
            var myPropInfo = myType.GetProperty(name: propertyName);
            myPropInfo.SetValue(this, value: value, null);
        }
    }
}