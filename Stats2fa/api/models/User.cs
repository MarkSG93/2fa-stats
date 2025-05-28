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