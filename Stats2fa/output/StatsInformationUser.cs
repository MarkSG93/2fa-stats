using System;
using System.Text.Json.Serialization;

namespace StatsBetter.Output;

public class StatsInformationUser {
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

    [JsonPropertyName("user_date")]
    public DateTime? UserCreatedTimestamp { get; set; }

    [JsonPropertyName("user_otp_type")]
    public string? TotpType { get; set; }

    [JsonPropertyName("user_otp_date")]
    public string? TotpDate { get; set; }

    [JsonPropertyName("user_otp_verified")]
    public bool? TotpVerified { get; set; }
}