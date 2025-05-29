using System;
using System.Text.Json.Serialization;

namespace StatsBetter.Output;

/// <summary>
///     All the Vendor, Distributor, Client information in one place.
/// </summary>
public class StatsInformationDistributorVendorClient {
    public int? StatsInformationId { get; set; }

    // client stuff
    [JsonPropertyName("client_date")]
    public DateTime? ClientCreatedTimestamp { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("client_type")]
    public string? ClientType { get; set; }

    [JsonPropertyName("client_status")]
    public string? ClientStatus { get; set; }

    [JsonPropertyName("client_vendor_id")]
    public string? ClientVendorId { get; set; }

    [JsonPropertyName("client_stats_status")]
    public string? ClientStatsStatus { get; set; }

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


    // vendor stuff
    [JsonPropertyName("vendor_date")]
    public DateTime? VendorCreatedTimestamp { get; set; }

    [JsonPropertyName("vendor_distributor_id")]
    public string? VendorDistributorId { get; set; }

    [JsonPropertyName("vendor_id")]
    public string? VendorId { get; set; }

    [JsonPropertyName("vendor_name")]
    public string? VendorName { get; set; }

    [JsonPropertyName("vendor_type")]
    public string? VendorType { get; set; }

    [JsonPropertyName("vendor_status")]
    public string? VendorStatus { get; set; }

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

    // distributor stuff
    [JsonPropertyName("distributor_date")]
    public DateTime? DistributorCreatedTimestamp { get; set; }

    [JsonPropertyName("distributor_id")]
    public string? DistributorId { get; set; }

    [JsonPropertyName("distributor_name")]
    public string? DistributorName { get; set; }

    [JsonPropertyName("distributor_type")]
    public string? DistributorType { get; set; }

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

    [JsonPropertyName("distributor_stats_status")]
    public string? DistributorStatsStatus { get; set; }

    // user stuff
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("user_name")]
    public string? Name { get; set; }

    [JsonPropertyName("user_email")]
    public string? Email { get; set; }

    [JsonPropertyName("user_mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("user_timezone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("user_language")]
    public string? Language { get; set; }

    [JsonPropertyName("user_state")]
    public string? State { get; set; }

    [JsonPropertyName("user_owner_id")]
    public string? OwnerId { get; set; }

    [JsonPropertyName("user_owner_name")]
    public string? OwnerName { get; set; }

    [JsonPropertyName("user_owner_type")]
    public string? OwnerType { get; set; }

    [JsonPropertyName("user_default_client_id")]
    public string? DefaultClientId { get; set; }

    [JsonPropertyName("user_default_client_name")]
    public string? DefaultClientName { get; set; }

    [JsonPropertyName("user_cost_centre_id")]
    public string? CostCentreId { get; set; }

    [JsonPropertyName("user_cost_centre_name")]
    public string? CostCentreName { get; set; }

    [JsonPropertyName("user_stats_status")]
    public string? UserStatsStatus { get; set; }

    [JsonPropertyName("user_modified_date")]
    public string? ModifiedDate { get; set; }

    [JsonPropertyName("user_timestamp")]
    public DateTime? UserCreatedTimestamp { get; set; }

    [JsonPropertyName("user_otp_type")]
    public string? TotpType { get; set; }

    [JsonPropertyName("user_otp_date")]
    public string? TotpDate { get; set; }

    [JsonPropertyName("user_otp_verified")]
    public bool? TotpVerified { get; set; }
}