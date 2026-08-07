namespace UberGenius.Api.Data;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }

    // Default operating timezone (IANA id, e.g. "America/Chicago") — captured automatically
    // from the browser at signup. Used as the fallback for any trip that doesn't carry its own
    // TimeZoneId (see Trip.cs), so day-of-week bucketing matches Uber's own rule.
    public string TimeZoneId { get; set; } = "";
}
