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
    
    // TODO support these additional fields that come from a different response but can be combined
    // {"id":"494c7592-eb05-4b17-a299-5ffaab5924d3","owner":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc","type":"client"},"name":"Johan Havenga (C)","emailAddress":"johanh@keytelematics.com","mobile":"+","timeZoneId":"Africa/Johannesburg","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"roles":[{"id":"00000000-0000-0000-0000-000000000000","name":"Administrator"}],"notifySettings":{"smsTime":{"to":"23:59:59","from":"00:00:00"},"actions":{"low":"none","medium":"none","high":"none"}},"costCentre":{"id":"4e66094e-9220-406c-87b4-e7e4408da089","name":"Gold Coast"},"oidcTags":{},"apiKeys":{},"otp":[],"entity":{"modifiedDate":"2025/05/28 12:08:58","creationDate":"2024/08/30 09:24:43"}}
    
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