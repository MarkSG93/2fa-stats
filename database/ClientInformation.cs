using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Stats2fa.database;

public class ClientInformation {
     public Int64 ClientInformationId { get; set; }

    // Non nullable info
    [JsonPropertyName("client_date")] public DateTime CreatedTimestamp { get; set; }
    [JsonPropertyName("client_vendor_id")] public string ClientVendorId { get; set; }
    [JsonPropertyName("client_id")] public string ClientId { get; set; }
    [JsonPropertyName("client_name")] public string ClientName { get; set; }
    [JsonPropertyName("client_type")] public string ClientType { get; set; }
    [JsonPropertyName("client_status")] public string ClientStatus { get; set; }

    // Nullable info

    public object this[string propertyName] {
        get {
            Type myType = typeof(ClientInformation);
            PropertyInfo myPropInfo = myType.GetProperty(propertyName);
            return myPropInfo.GetValue(this, null);
        }
        set {
            Type myType = typeof(ClientInformation);
            PropertyInfo myPropInfo = myType.GetProperty(propertyName);
            myPropInfo.SetValue(this, value, null);
        }
    }
}