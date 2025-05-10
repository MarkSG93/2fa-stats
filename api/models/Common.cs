using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stats2fa.api.models;

public class Common {
    public class Source {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
    }

    public class PasswordPolicy {
        [JsonPropertyName("source")] public Source? Source { get; set; }
        [JsonPropertyName("passwordLength")] public int PasswordLength { get; set; }

        [JsonPropertyName("passwordComplexity")] public PasswordComplexity? PasswordComplexity { get; set; }

        [JsonPropertyName("passwordExpirationDays")] public int PasswordExpirationDays { get; set; }

        [JsonPropertyName("otpSettings")] public OtpSettings? OtpSettings { get; set; }
    }

    public class Owner {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    public class PasswordComplexity {
        [JsonPropertyName("mixedCase")] public bool MixedCase { get; set; }
        [JsonPropertyName("alphaNumerical")] public bool AlphaNumerical { get; set; }

        [JsonPropertyName("noCommonPasswords")] public bool NoCommonPasswords { get; set; }

        [JsonPropertyName("specialCharacters")] public bool SpecialCharacters { get; set; }
    }

    public class OtpSettings {
        [JsonPropertyName("methods")] public OtpMethods Methods { get; set; }
        [JsonPropertyName("gracePeriodDays")] public int GracePeriodDays { get; set; }
        [JsonPropertyName("mandatoryFor")] public string MandatoryFor { get; set; }
    }

    public class OtpMethods {
        [JsonPropertyName("totp")] public TokenValidity Totp { get; set; }
        [JsonPropertyName("email")] public TokenValidity Email { get; set; }
    }

    public class TokenValidity {
        [JsonPropertyName("tokenValidityDays")] public int TokenValidityDays { get; set; }
    }

    public class Features {
        [JsonPropertyName("providerFeatures")] public List<object> ProviderFeatures { get; set; }
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
        [JsonPropertyName("enableNotifications")] public bool? EnableNotifications { get; set; }
        [JsonPropertyName("maxOverviewAssets")] public int? MaxOverviewAssets { get; set; }
    }



    public class StatsFilter {
        [JsonPropertyName("groupLevel")] public string groupLevel { get; set; }
        [JsonPropertyName("rowLevel")] public string rowLevel { get; set; }
        [JsonPropertyName("flip")] public bool flip { get; set; }
        [JsonPropertyName("time")] public string time { get; set; }
    }

    public class MapLayers {
        [JsonPropertyName("items")] public List<Item> Items { get; set; }

        public class Item {
            [JsonPropertyName("id")] public string Id { get; set; }
            [JsonPropertyName("name")] public string Name { get; set; }
        }
    }

    public class Stats {
        public class Average {
            [JsonPropertyName("value")] public List<string> Value { get; set; }
            [JsonPropertyName("raw")] public List<double> Raw { get; set; }
        }

        public class Cellset {
            [JsonPropertyName("status")] public string Status { get; set; }
            [JsonPropertyName("average")] public Average Average { get; set; }
        }

        [JsonPropertyName("cellset")] public Cellset CellSet { get; set; }
    }

    public class Address {
        [JsonPropertyName("address")] public string? Address1 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
    }

    public class Theme {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("settings")] public Dictionary<string, object>? Settings { get; set; }
    }

    public class MapSet {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    public class AvailableMapSet {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }
    
    public class Support {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
    }

    public class Messages {
        [JsonPropertyName("login")] public string? Login { get; set; }
        [JsonPropertyName("suspended")] public string? Suspended { get; set; }
    }

    public class Limits {
        [JsonPropertyName("entities")] public Entities? Entities { get; set; }
    }

    public class Entity {
        [JsonPropertyName("creationDate")] public string? CreationDate { get; set; }
        [JsonPropertyName("modifiedDate")] public string? ModifiedDate { get; set; }
    }

    public class EmailProvider {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
    }

    public class Retention {
        [JsonPropertyName("source")] public Common.Source Source { get; set; }
        [JsonPropertyName("retainFor")] public int RetainFor { get; set; }
        [JsonPropertyName("retainForUnit")] public string RetainForUnit { get; set; }
        [JsonPropertyName("horizonDate")] public string HorizonDate { get; set; }
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

    public class CustomField {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("values")] public List<string>? Values { get; set; }
        [JsonPropertyName("required")] public bool Required { get; set; }
        [JsonPropertyName("owner")] public string Owner { get; set; }
    }

    public class HistoryFeature : Common.FeatureConfig {
        [JsonPropertyName("tripMode")] public string? TripMode { get; set; }
        [JsonPropertyName("showReplayLines")] public bool? ShowReplayLines { get; set; }
        [JsonPropertyName("telemetryEnabled")] public bool? TelemetryEnabled { get; set; }

        [JsonPropertyName("tripReplayEnabled")] public bool? TripReplayEnabled { get; set; }
    }

    public class ReportsFeature : Common.FeatureConfig {
        [JsonPropertyName("reports")] public List<string>? Reports { get; set; }
        [JsonPropertyName("previewMode")] public string? PreviewMode { get; set; }
        [JsonPropertyName("reportServer")] public string? ReportServer { get; set; }
    }

    public class TrackingFeature : Common.FeatureConfig {
        [JsonPropertyName("loadShared")] public bool? LoadShared { get; set; }
        [JsonPropertyName("enableRoutes")] public bool? EnableRoutes { get; set; }
        [JsonPropertyName("enablePolling")] public bool? EnablePolling { get; set; }
    }

    public class AnalyticsFeature : Common.FeatureConfig {
        [JsonPropertyName("enableReports")] public bool? EnableReports { get; set; }
        [JsonPropertyName("enableOlap")] public bool? EnableOlap { get; set; }
        [JsonPropertyName("assetGroups")] public object? AssetGroups { get; set; }
    }

    public class RoadSpeedFeature : Common.FeatureConfig {
        [JsonPropertyName("assetGroups")] public object? AssetGroups { get; set; }
    }

    public class NotificationsFeature : Common.FeatureConfig {
        [JsonPropertyName("enablePopupMessages")] public bool? EnablePopupMessages { get; set; }
        [JsonPropertyName("enableAssetNotifications")] public bool? EnableAssetNotifications { get; set; }
    }

    public class SslCertificate {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("domain")] public string Domain { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("modifiedDate")] public string ModifiedDate { get; set; }
    }

    public class EntityLimit {
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("active")] public int Active { get; set; }
        [JsonPropertyName("max")] public int Max { get; set; }
    }


    public class Entities {
        [JsonPropertyName("vendor")] public EntityLimit Vendor { get; set; }

        [JsonPropertyName("dashboardtemplate")] public EntityLimit DashboardTemplate { get; set; }

        [JsonPropertyName("themeconfig")] public EntityLimit ThemeConfig { get; set; }

        [JsonPropertyName("user")] public EntityLimit? User { get; set; }
        [JsonPropertyName("userrole")] public EntityLimit? UserRole { get; set; }
        [JsonPropertyName("alert")] public EntityLimit? Alert { get; set; }
        [JsonPropertyName("assetcategory")] public EntityLimit? AssetCategory { get; set; }
        [JsonPropertyName("assetgroup")] public EntityLimit? AssetGroup { get; set; }

        [JsonPropertyName("assetratingprofile")] public EntityLimit? AssetRatingProfile { get; set; }

        [JsonPropertyName("asset")] public EntityLimit? Asset { get; set; }

        [JsonPropertyName("assetstateprofile")] public EntityLimit? AssetStateProfile { get; set; }

        [JsonPropertyName("assettag")] public EntityLimit? AssetTag { get; set; }
        [JsonPropertyName("accessgroup")] public EntityLimit? AccessGroup { get; set; }
        [JsonPropertyName("dashboard")] public EntityLimit? Dashboard { get; set; }
        [JsonPropertyName("device")] public EntityLimit? Device { get; set; }

        [JsonPropertyName("deviceconfigprofile")] public EntityLimit? DeviceConfigProfile { get; set; }

        [JsonPropertyName("deviceprovider")] public EntityLimit? DeviceProvider { get; set; }
        [JsonPropertyName("emailprovider")] public EntityLimit? EmailProvider { get; set; }
        [JsonPropertyName("fuelcard")] public EntityLimit? FuelCard { get; set; }
        [JsonPropertyName("geolockprofile")] public EntityLimit? GeoLockProfile { get; set; }
        [JsonPropertyName("inputoutputtype")] public EntityLimit? InputOutputType { get; set; }
        [JsonPropertyName("label")] public EntityLimit? Label { get; set; }
        [JsonPropertyName("mapset")] public EntityLimit? MapSet { get; set; }
        [JsonPropertyName("overspeedprofile")] public EntityLimit? OverSpeedProfile { get; set; }
        [JsonPropertyName("privacyprofile")] public EntityLimit? PrivacyProfile { get; set; }
        [JsonPropertyName("reporttemplate")] public EntityLimit? ReportTemplate { get; set; }
        [JsonPropertyName("reminder")] public EntityLimit? Reminder { get; set; }
        [JsonPropertyName("scheduledreport")] public EntityLimit? ScheduledReport { get; set; }
        [JsonPropertyName("simcard")] public EntityLimit? SimCard { get; set; }

        [JsonPropertyName("smsgatewayprovider")] public EntityLimit? SmsGatewayProvider { get; set; }

        [JsonPropertyName("zonegroup")] public EntityLimit? ZoneGroup { get; set; }
        [JsonPropertyName("zone")] public EntityLimit? Zone { get; set; }
    }
}