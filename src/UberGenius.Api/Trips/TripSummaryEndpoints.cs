using Microsoft.EntityFrameworkCore;
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
        app.MapGet("/api/trips/summary", async (AppDbContext db) =>
        {
            // Cancelled trips have Earnings = 0 / DistanceMiles = 0 by design (no fare was
            // ever charged) — including them here would dilute the per-trip/per-mile
            // averages with entries that aren't really "a trip" from an earnings standpoint.
            var completed = db.Trips.Where(t => t.Status == "completed");

            var aggregate = await completed
                .GroupBy(t => 1)
                .Select(g => new
                {
                    TotalTrips = g.Count(),
                    TotalEarnings = g.Sum(t => t.Earnings),
                    TotalMiles = g.Sum(t => t.DistanceMiles),
                    // Request time (not pickup time) is the start boundary — pickup-to-dropoff
                    // alone excludes the drive-to-pickup time and understates true active time.
                    // Doesn't yet correct for back-to-back trips accepted before the previous
                    // one ended (~47% of trips, confirmed against real data) — see the
                    // follow-up plan for the overlap-corrected version.
                    TotalDrivingMinutes = g.Sum(t => EF.Functions.DateDiffMinute(t.RequestedTimeUtc ?? t.StartTimeUtc, t.EndTimeUtc)),
                })
                .FirstOrDefaultAsync();

            if (aggregate is null || aggregate.TotalTrips == 0)
            {
                return Results.Ok(new TripSummary(0, 0m, 0m, 0m, 0m, 0m, 0m));
            }

            var totalDrivingHours = aggregate.TotalDrivingMinutes / 60m;

            var summary = new TripSummary(
                TotalTrips: aggregate.TotalTrips,
                TotalEarnings: aggregate.TotalEarnings,
                AverageEarningsPerTrip: aggregate.TotalEarnings / aggregate.TotalTrips,
                TotalMiles: aggregate.TotalMiles,
                AverageEarningsPerMile: aggregate.TotalMiles == 0 ? 0m : aggregate.TotalEarnings / aggregate.TotalMiles,
                EstimatedHourlyEarnings: totalDrivingHours == 0 ? 0m : aggregate.TotalEarnings / totalDrivingHours,
                TotalDrivingHours: totalDrivingHours);

            return Results.Ok(summary);
        });
    }
}
