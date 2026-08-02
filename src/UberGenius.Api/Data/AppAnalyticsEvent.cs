namespace UberGenius.Api.Data;

// Trimmed to fields with plausible future use (map/GPS, online-time analytics).
// Device/carrier metadata (cellular carrier, device model/OS, IP address, etc.) is
// deliberately not stored — pure telemetry noise with no use in this app.
public class AppAnalyticsEvent : IUserOwned
{
    public long Id { get; set; }
    public int UserId { get; set; }

    public string EventName { get; set; } = "";
    public string? EventType { get; set; }
    public string? City { get; set; }
    public bool? IsDriverOnline { get; set; }
    public string? DriverStatus { get; set; }
    public DateTime EventTimeUtc { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? SpeedGps { get; set; }
}
