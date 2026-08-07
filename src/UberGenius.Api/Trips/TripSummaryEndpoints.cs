using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UberGenius.Api.Auth;
using UberGenius.Api.Data;

namespace UberGenius.Api.Trips;

public record TripSummary(
    int TotalTrips,
    decimal TotalEarnings,
    decimal AverageEarningsPerTrip,
    decimal TotalMiles,
    decimal AverageEarningsPerMile,
    decimal EstimatedHourlyEarnings,
    decimal TotalDrivingHours);

public static class TripSummaryEndpoints
{
    public static void MapTripSummaryEndpoints(this WebApplication app)
    {
        app.MapGet("/api/trips/summary", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            // No Status filter, deliberately: a cancelled trip can still carry a real matched
            // cancellation fee (confirmed against real data — Status alone doesn't tell you
            // whether money changed hands), and Trip.Earnings is already the right source of
            // truth either way (0 when genuinely unpaid, the real amount otherwise). Filtering
            // on Status == "completed" here previously undercounted all-time earnings by
            // $297.02 across 56 trips.
            var userId = principal.GetUserId();

            // Pulled into memory and folded in C# (trivial volume — a few thousand rows per
            // user) rather than summed in SQL: driving time has to be overlap-corrected, since
            // ~47% of completed trips (confirmed against real data) are accepted before the
            // previous one ends, and a plain per-trip sum would double-count that overlap.
            // Ordered by RequestedTimeUtc, the same field the fold below anchors on — every
            // real completed trip has one (confirmed: 0 nulls across 2,346 trips), so no
            // StartTimeUtc fallback here. A null would mean the source file had no request-time
            // column at all, a real data-shape problem worth failing loudly on rather than
            // silently substituting a different anchor for.
            var orderedTrips = await db.Trips
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.RequestedTimeUtc)
                .Select(t => new { t.Id, t.RequestedTimeUtc, t.EndTimeUtc, t.Earnings, t.DistanceMiles })
                .ToListAsync();

            if (orderedTrips.Count == 0)
            {
                return Results.Ok(new TripSummary(0, 0m, 0m, 0m, 0m, 0m, 0m));
            }

            var totalEarnings = 0m;
            var totalMiles = 0m;
            var totalDrivingMinutes = 0.0;
            DateTime? runningEnd = null;

            foreach (var t in orderedTrips)
            {
                totalEarnings += t.Earnings;
                totalMiles += t.DistanceMiles;

                // Request time (not pickup time) is the start boundary — pickup-to-dropoff
                // alone excludes the drive-to-pickup time and understates true active time.
                // The effective start is clamped forward to the end of whatever's already
                // been counted, so a trip accepted before the previous one ends only
                // contributes its non-overlapping tail, never double-counted minutes.
                var anchorStart = t.RequestedTimeUtc
                    ?? throw new InvalidOperationException($"Trip {t.Id} has no RequestedTimeUtc.");
                var effectiveStart = runningEnd.HasValue && runningEnd.Value > anchorStart ? runningEnd.Value : anchorStart;

                if (t.EndTimeUtc > effectiveStart)
                {
                    totalDrivingMinutes += (t.EndTimeUtc - effectiveStart).TotalMinutes;
                }

                // A running max, not just the latest trip's end — guards a trip that's fully
                // nested inside a longer one still in progress from pulling the boundary back.
                runningEnd = runningEnd.HasValue && runningEnd.Value > t.EndTimeUtc ? runningEnd.Value : t.EndTimeUtc;
            }

            var totalDrivingHours = (decimal)totalDrivingMinutes / 60m;
            var totalTrips = orderedTrips.Count;

            var summary = new TripSummary(
                TotalTrips: totalTrips,
                TotalEarnings: totalEarnings,
                AverageEarningsPerTrip: totalEarnings / totalTrips,
                TotalMiles: totalMiles,
                AverageEarningsPerMile: totalMiles == 0 ? 0m : totalEarnings / totalMiles,
                EstimatedHourlyEarnings: totalDrivingHours == 0 ? 0m : totalEarnings / totalDrivingHours,
                TotalDrivingHours: totalDrivingHours);

            return Results.Ok(summary);
        }).RequireAuthorization();
    }
}
