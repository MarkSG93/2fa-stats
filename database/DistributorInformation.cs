using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Stats2fa.database;

public class DistributorInformation {
    public Int64 DistributorInformationId { get; set; }

    // Non nullable info
    [JsonPropertyName("distributor_date")] public DateTime CreatedTimestamp { get; set; }
    [JsonPropertyName("distributor_id")] public string DistributorId { get; set; }
    [JsonPropertyName("distributor_name")] public string DistributorName { get; set; }
    [JsonPropertyName("distributor_type")] public string DistributorType { get; set; }

    // Nullable info
    [JsonPropertyName("distributor_status")]
    public string? distributorStatus { get; set; }

    public object this[string propertyName]
    {
        get
        {
            Type myType = typeof(DistributorInformation);
            PropertyInfo myPropInfo = myType.GetProperty(propertyName);
            return myPropInfo.GetValue(this, null);
        }
        set
        {
            Type myType = typeof(DistributorInformation);
            PropertyInfo myPropInfo = myType.GetProperty(propertyName);
            myPropInfo.SetValue(this, value, null);
        }
    }
}