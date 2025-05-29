using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class User {
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("owner")]
    [NotMapped]
    public Common.Owner? Owner { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("timeZoneId")]
    public string? TimeZoneId { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("defaultClient")]
    [NotMapped]
    public UserDefaultClient? DefaultClient { get; set; }

    [JsonPropertyName("costCentre")]
    [NotMapped]
    public UserCostCentre? CostCentre { get; set; }

    [JsonPropertyName("modifiedDate")]
    public string? ModifiedDate { get; set; }

    // Additional fields from the extended response
    [JsonPropertyName("roles")]
    [NotMapped]
    public List<UserRole>? Roles { get; set; }

    [JsonPropertyName("notifySettings")]
    [NotMapped]
    public NotifySettings? UserNotifySettings { get; set; }

    [JsonPropertyName("oidcTags")]
    [NotMapped]
    public Dictionary<string, object>? OidcTags { get; set; }

    [JsonPropertyName("apiKeys")]
    [NotMapped]
    public Dictionary<string, object>? ApiKeys { get; set; }

    [JsonPropertyName("otp")]
    [NotMapped]
    public List<OtpInfo>? Otp { get; set; }

    [JsonPropertyName("entity")]
    [NotMapped]
    public UserEntity? Entity { get; set; }

    public class UserDefaultClient {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class UserCostCentre {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("err")]
        [NotMapped]
        public ErrorDetails? Error { get; set; }
    }

    public class UserRole {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class NotifySettings {
        [JsonPropertyName("smsTime")]
        public SmsTime? UserSmsTime { get; set; }

        [JsonPropertyName("actions")]
        public NotifyActions? Actions { get; set; }

        public class SmsTime {
            [JsonPropertyName("to")]
            public string To { get; set; }

            [JsonPropertyName("from")]
            public string From { get; set; }
        }

        public class NotifyActions {
            [JsonPropertyName("low")]
            public string Low { get; set; }

            [JsonPropertyName("medium")]
            public string Medium { get; set; }

            [JsonPropertyName("high")]
            public string High { get; set; }
        }
    }

    public class UserEntity {
        [JsonPropertyName("modifiedDate")]
        public string ModifiedDate { get; set; }

        [JsonPropertyName("creationDate")]
        public string CreationDate { get; set; }
    }

    public class OtpInfo {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
    }

    // Use a struct instead of a class to avoid EF treating it as an entity
    public struct ErrorDetails {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}

internal class UserComparer : IEqualityComparer<User> {
    public static readonly UserComparer Instance = new();

    public bool Equals(User x, User y) {
        return x.Id.Equals(value: y.Id);
    }

    public int GetHashCode(User obj) {
        return obj.Id.GetHashCode();
    }
}