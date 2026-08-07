using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UberGenius.Api.Auth;
using UberGenius.Api.Data;

namespace UberGenius.Api.Trips;

public record DayOfWeekEarnings(string Label, decimal Total, int TripCount);

public static class TripEarningsByDayEndpoints
{
    private static readonly string[] DayLabels = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    public static void MapTripEarningsByDayEndpoints(this WebApplication app)
    {
        app.MapGet("/api/trips/earnings-by-day", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userId = principal.GetUserId();

            var userTimeZoneId = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.TimeZoneId)
                .SingleAsync();

            // No Status filter, deliberately — see TripSummaryEndpoints.cs for why: a cancelled
            // trip can still carry a real matched cancellation fee, and Trip.Earnings is already
            // 0 for genuinely unpaid trips either way.
            var rows = await db.Trips
                .Where(t => t.UserId == userId)
                .Select(t => new { t.Id, t.RequestedTimeUtc, t.Earnings, t.TimeZoneId })
                .ToListAsync();

            var totals = new decimal[7];
            var counts = new int[7];

            foreach (var t in rows)
            {
                // Anchored on RequestedTime, not StartTime — matches Uber's own rule and the
                // same no-fallback precedent as TripSummaryEndpoints.cs: a null here would mean
                // the source file had no request-time column at all, worth failing loudly on.
                var requestedTimeUtc = t.RequestedTimeUtc
                    ?? throw new InvalidOperationException($"Trip {t.Id} has no RequestedTimeUtc.");

                // Trip's own timezone if the export provided one, else the driver's account
                // default — see OperatingDay.cs and Trip.TimeZoneId for why both exist.
                var timeZoneId = t.TimeZoneId ?? userTimeZoneId;
                var index = (int)OperatingDay.FromUtc(requestedTimeUtc, timeZoneId).DayOfWeek;
                totals[index] += t.Earnings;
                counts[index]++;
            }

            var result = Enumerable.Range(0, 7)
                .Select(i => new DayOfWeekEarnings(DayLabels[i], totals[i], counts[i]))
                .ToList();

            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
