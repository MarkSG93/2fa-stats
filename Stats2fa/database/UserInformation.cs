using System;
using System.Text.Json.Serialization;
using Stats2fa.api.models;

namespace Stats2fa.database;

public class UserInformation {
    // Primary key
    public long UserInformationId { get; set; }

    // User's API ID (from the API response)
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    // Basic user information
    [JsonPropertyName("user_name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("user_email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("user_mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("user_timezone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("user_language")]
    public string? Language { get; set; }

    [JsonPropertyName("user_state")]
    public string? State { get; set; }

    // Owner information
    [JsonPropertyName("user_owner_id")]
    public string OwnerId { get; set; } = string.Empty;

    [JsonPropertyName("user_owner_name")]
    public string OwnerName { get; set; } = string.Empty;

    [JsonPropertyName("user_owner_type")]
    public string OwnerType { get; set; } = string.Empty;

    // Default client information
    [JsonPropertyName("user_default_client_id")]
    public string DefaultClientId { get; set; } = string.Empty;

    [JsonPropertyName("user_default_client_name")]
    public string DefaultClientName { get; set; } = string.Empty;

    // Cost center information
    [JsonPropertyName("user_cost_centre_id")]
    public string? CostCentreId { get; set; } = string.Empty;

    [JsonPropertyName("user_cost_centre_name")]
    public string? CostCentreName { get; set; } = string.Empty;

    // Dates
    [JsonPropertyName("user_modified_date")]
    public string? ModifiedDate { get; set; }

    [JsonPropertyName("created_timestamp")]
    public DateTime CreatedTimestamp { get; set; }
}