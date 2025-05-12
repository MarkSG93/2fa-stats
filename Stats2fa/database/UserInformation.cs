using System;
using System.Text.Json.Serialization;
using Stats2fa.api.models;

namespace Stats2fa.database;

public class UserInformation {
    // Primary key
    public long UserId { get; set; }

    // User's API ID (from the API response)
    [JsonPropertyName("user_id")]
    public string ApiUserId { get; set; } = string.Empty;

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

    // Dates
    [JsonPropertyName("user_modified_date")]
    public string? ModifiedDate { get; set; }

    [JsonPropertyName("created_timestamp")]
    public DateTime CreatedTimestamp { get; set; }

    // Client relationship
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    // Foreign key to ClientInformation
    [JsonPropertyName("client_information_id")]
    public long? ClientInformationId { get; set; }

    [JsonIgnore]
    public ClientInformation? Client { get; set; }

    [JsonIgnore] // Ignore this for EF Core
    public Users? UserData { get; set; }
}