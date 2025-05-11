using System;
using System.Text.Json.Serialization;
using Stats2fa.api.models;

namespace Stats2fa.database;

public class UserInformation {
    public long UserInformationId { get; set; }

    // Non nullable info
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("user_email")]
    public string UserEmail { get; set; } = string.Empty;

    // Nullable info
    [JsonPropertyName("user_mobile")]
    public string? UserMobile { get; set; }

    [JsonPropertyName("user_timezone")]
    public string? UserTimezone { get; set; }

    [JsonPropertyName("user_language")]
    public string? UserLanguage { get; set; }

    [JsonPropertyName("user_state")]
    public string? UserState { get; set; }

    [JsonPropertyName("user_modified_date")]
    public string? UserModifiedDate { get; set; }

    [JsonPropertyName("user_date")]
    public DateTime CreatedTimestamp { get; set; }

    [JsonIgnore] // Ignore this for EF Core
    public Users? UserData { get; set; }
}