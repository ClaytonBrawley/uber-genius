using Microsoft.EntityFrameworkCore;
using UberGenius.Api.Data;

namespace UberGenius.Api.Trips;

// Trips have no GPS data at all — only AppAnalyticsEvents does, and only for trips whose
// start time falls within the App Analytics coverage window (confirmed ~1 month of real
// data). Pickup/dropoff location is approximated as the nearest GPS event in time to the
// trip's start/end, same nearest-neighbor-in-time pattern as TripPaymentMatcher. A tolerance
// keeps this honest: no event within 5 minutes means no location, not a wrong guess.
public record TripMapPoint(
    int Id,
    DateTime StartTimeUtc,
    decimal Earnings,
    string? City,
    double? PickupLatitude,
    double? PickupLongitude,
    double? DropoffLatitude,
    double? DropoffLongitude);

public static class TripMapEndpoints
{
    private const double MaxToleranceMinutes = 5;

    public static void MapTripMapEndpoints(this WebApplication app)
    {
        app.MapGet("/api/trips/map-points", async (AppDbContext db) =>
        {
            var events = await db.AppAnalyticsEvents
                .Where(e => e.Latitude != null && e.Longitude != null)
                .OrderBy(e => e.EventTimeUtc)
                .Select(e => new { e.EventTimeUtc, e.Latitude, e.Longitude })
                .ToListAsync();

            if (events.Count == 0)
            {
                return Results.Ok(Array.Empty<TripMapPoint>());
            }

            var eventTimes = events.Select(e => e.EventTimeUtc).ToList();

            (double? Lat, double? Lng, double DeltaMinutes) FindNearest(DateTime target)
            {
                var index = eventTimes.BinarySearch(target);
                if (index < 0)
                {
                    index = ~index; // insertion point: first event >= target
                }

                var candidateIndexes = new List<int>();
                if (index < eventTimes.Count)
                {
                    candidateIndexes.Add(index);
                }
                if (index > 0)
                {
                    candidateIndexes.Add(index - 1);
                }

                var best = candidateIndexes
                    .Select(i => (i, delta: Math.Abs((eventTimes[i] - target).TotalMinutes)))
                    .OrderBy(c => c.delta)
                    .First();

                var e = events[best.i];
                return (e.Latitude, e.Longitude, best.delta);
            }

            // Only trips starting within the coverage window have any chance of a nearby event.
            var windowStart = eventTimes[0];
            var windowEnd = eventTimes[^1];

            var trips = await db.Trips
                .Where(t => t.Status == "completed" && t.StartTimeUtc >= windowStart && t.StartTimeUtc <= windowEnd)
                .Select(t => new { t.Id, t.StartTimeUtc, t.EndTimeUtc, t.Earnings, t.City })
                .ToListAsync();

            var points = new List<TripMapPoint>();
            foreach (var t in trips)
            {
                var pickup = FindNearest(t.StartTimeUtc);
                var dropoff = FindNearest(t.EndTimeUtc);

                var pickupOk = pickup.DeltaMinutes <= MaxToleranceMinutes;
                var dropoffOk = dropoff.DeltaMinutes <= MaxToleranceMinutes;

                if (!pickupOk && !dropoffOk)
                {
                    continue; // no reliable location at all — skip rather than guess
                }

                points.Add(new TripMapPoint(
                    t.Id, t.StartTimeUtc, t.Earnings, t.City,
                    pickupOk ? pickup.Lat : null,
                    pickupOk ? pickup.Lng : null,
                    dropoffOk ? dropoff.Lat : null,
                    dropoffOk ? dropoff.Lng : null));
            }

            return Results.Ok(points);
        });
    }
}
