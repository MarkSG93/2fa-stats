using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class Vendor {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("website")] public string? Website { get; set; }
    [JsonPropertyName("timeZoneId")] public string? TimeZoneId { get; set; }
    [JsonPropertyName("language")] public string? Language { get; set; }
    [JsonPropertyName("pin")] public string? Pin { get; set; }
    [JsonPropertyName("group")] public string? Group { get; set; }

    [JsonPropertyName("availableMapSets")] public List<Common.AvailableMapSet>? AvailableMapSets { get; set; }

    [JsonPropertyName("availableDeviceTypes")]
    public List<object>? AvailableDeviceTypes { get; set; }

    [JsonPropertyName("defaultMapSet")] public Common.AvailableMapSet? DefaultMapSet { get; set; }
    [JsonPropertyName("mapSet")] public MapSet? MapSet { get; set; }

    [JsonPropertyName("owner")] public Common.Owner? owner { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("theme")] public Theme? Theme { get; set; }
    [JsonPropertyName("customFields")] public Dictionary<string, List<CustomField>>? CustomFields { get; set; }
    [JsonPropertyName("domains")] public List<string>? Domains { get; set; }
    [JsonPropertyName("address")] public Address? Address { get; set; }
    [JsonPropertyName("support")] public Support? Support { get; set; }
    [JsonPropertyName("messages")] public Messages? Messages { get; set; }
    [JsonPropertyName("limits")] public Limits? Limits { get; set; }
    [JsonPropertyName("flags")] public Dictionary<string, object>? Flags { get; set; }
    [JsonPropertyName("oidc")] public Dictionary<string, object>? Oidc { get; set; }
    [JsonPropertyName("entity")] public Entity? Entity { get; set; }
    [JsonPropertyName("emailProvider")] public EmailProvider? EmailProvider { get; set; }
    [JsonPropertyName("sslCertificates")] public List<SslCertificate>? SslCertificates { get; set; }
    [JsonPropertyName("retention")] public Retention? Retention { get; set; }
    [JsonPropertyName("passwordPolicy")] public Common.PasswordPolicy? passwordPolicy { get; set; }
    [JsonPropertyName("features")] public Features? Features { get; set; }
    [JsonPropertyName("measurementUnits")] public MeasurementUnits? MeasurementUnits { get; set; }
    [JsonPropertyName("meta")] public Meta? Meta { get; set; }
}

// Reusing classes from Common.cs when possible
public class MapSet {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
}

public class CustomField {
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; }
    [JsonPropertyName("values")] public List<string> Values { get; set; }
    [JsonPropertyName("required")] public bool Required { get; set; }
    [JsonPropertyName("owner")] public string Owner { get; set; }
}

public class Address {
    [JsonPropertyName("address")] public string? Address1 { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("code")] public string? Code { get; set; }
}

public class MeasurementUnits {
    [JsonPropertyName("distanceUnit")] public string? DistanceUnit { get; set; }
    [JsonPropertyName("altitudeUnit")] public string? AltitudeUnit { get; set; }
    [JsonPropertyName("speedUnit")] public string? SpeedUnit { get; set; }
    [JsonPropertyName("areaUnit")] public string? AreaUnit { get; set; }
    [JsonPropertyName("volumeUnit")] public string? VolumeUnit { get; set; }
    [JsonPropertyName("weightUnit")] public string? WeightUnit { get; set; }
    [JsonPropertyName("timeUnit")] public string? TimeUnit { get; set; }
    [JsonPropertyName("dateUnit")] public string? DateUnit { get; set; }
    [JsonPropertyName("temperatureUnit")] public string? TemperatureUnit { get; set; }
}

public class Meta {
    [JsonPropertyName("currentMapSetId")] public string? CurrentMapSetId { get; set; }
    [JsonPropertyName("mergedFlags")] public Dictionary<string, Dictionary<string, object>>? MergedFlags { get; set; }
}

public class Features {
    [JsonPropertyName("alerts")] public FeatureConfig? Alerts { get; set; }
    [JsonPropertyName("limits")] public FeatureConfig? Limits { get; set; }
    [JsonPropertyName("billing")] public FeatureConfig? Billing { get; set; }
    [JsonPropertyName("history")] public HistoryFeature? History { get; set; }
    [JsonPropertyName("reports")] public ReportsFeature? Reports { get; set; }
    [JsonPropertyName("tracking")] public TrackingFeature? Tracking { get; set; }
    [JsonPropertyName("analytics")] public AnalyticsFeature? Analytics { get; set; }
    [JsonPropertyName("roadSpeed")] public RoadSpeedFeature? RoadSpeed { get; set; }
    [JsonPropertyName("customTabs")] public FeatureConfig? CustomTabs { get; set; }
    [JsonPropertyName("assetRating")] public FeatureConfig? AssetRating { get; set; }
    [JsonPropertyName("notifications")] public NotificationsFeature? Notifications { get; set; }
}

public class FeatureConfig {
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("parameters")] public Dictionary<string, object>? Parameters { get; set; }
    [JsonPropertyName("enableAlertIcon")] public bool? EnableAlertIcon { get; set; }

    [JsonPropertyName("enableNotifications")]
    public bool? EnableNotifications { get; set; }

    [JsonPropertyName("maxOverviewAssets")]
    public int? MaxOverviewAssets { get; set; }
}

public class HistoryFeature : FeatureConfig {
    [JsonPropertyName("tripMode")] public string? TripMode { get; set; }
    [JsonPropertyName("showReplayLines")] public bool? ShowReplayLines { get; set; }
    [JsonPropertyName("telemetryEnabled")] public bool? TelemetryEnabled { get; set; }

    [JsonPropertyName("tripReplayEnabled")]
    public bool? TripReplayEnabled { get; set; }
}

public class ReportsFeature : FeatureConfig {
    [JsonPropertyName("reports")] public List<string>? Reports { get; set; }
    [JsonPropertyName("previewMode")] public string? PreviewMode { get; set; }
    [JsonPropertyName("reportServer")] public string? ReportServer { get; set; }
}

public class TrackingFeature : FeatureConfig {
    [JsonPropertyName("loadShared")] public bool? LoadShared { get; set; }
    [JsonPropertyName("enableRoutes")] public bool? EnableRoutes { get; set; }
    [JsonPropertyName("enablePolling")] public bool? EnablePolling { get; set; }
}

public class AnalyticsFeature : FeatureConfig {
    [JsonPropertyName("enableReports")] public bool? EnableReports { get; set; }
    [JsonPropertyName("enableOlap")] public bool? EnableOlap { get; set; }
    [JsonPropertyName("assetGroups")] public object? AssetGroups { get; set; }
}

public class RoadSpeedFeature : FeatureConfig {
    [JsonPropertyName("assetGroups")] public object? AssetGroups { get; set; }
}

public class NotificationsFeature : FeatureConfig {
    [JsonPropertyName("enablePopupMessages")]
    public bool? EnablePopupMessages { get; set; }

    [JsonPropertyName("enableAssetNotifications")]
    public bool? EnableAssetNotifications { get; set; }
}