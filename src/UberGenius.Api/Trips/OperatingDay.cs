namespace UberGenius.Api.Trips;

// Uber's own driver-app day-bucketing rule — empirically confirmed against two real trips
// checked directly in the Uber app: a trip requested 2025-09-05 03:27 AM Central shows under
// Thursday 9/4; a trip requested the same morning at 04:06 AM Central (39 minutes later) shows
// under Friday 9/5. The boundary is 4:00 AM local time (not midnight), anchored on when the
// trip was requested (not when it started).
//
// Takes the timezone as a required parameter rather than assuming one — this decides which of
// Uber's calendar-day buckets a trip belongs to, a fixed fact about the trip itself, not a
// per-viewer display preference. The caller resolves the right timezone (trip-level if the
// export provided one, else the driver's account default) before calling this.
public static class OperatingDay
{
    private static readonly TimeSpan CutoffTimeOfDay = TimeSpan.FromHours(4);

    public static DateOnly FromUtc(DateTime utcTime, string timeZoneId)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcTime, zone);
        return local.TimeOfDay < CutoffTimeOfDay
            ? DateOnly.FromDateTime(local.AddDays(-1))
            : DateOnly.FromDateTime(local);
    }
}
