using Microsoft.EntityFrameworkCore;
using UberGenius.Api.Data;

namespace UberGenius.Api.Imports;

public record TripPaymentMatchStatistics(
    int TripsConfidentMatch,
    int TripsApproximateMatch,
    int TripsUnmatched,
    int TripsCancelled,
    int PaymentGroupsMatched,
    int PaymentGroupsUnmatched);

// Trips have no unique ID and Payments have no reference back to Trips other than
// city + a per-trip-group timestamp, so this is a best-effort greedy nearest-neighbor
// match, not a key join. Match() itself only ever sees whatever list it's handed — see
// LoadCandidatesAsync for how that list is built to also cover previously-persisted,
// still-unmatched rows from earlier submissions (relevant now that re-imports are
// incremental: a payment for last week's trip can show up in this week's upload).
public static class TripPaymentMatcher
{
    // Builds the candidate set for one submission: the batch's newly-inserted trips/
    // payments, plus any of the user's still-Unmatched trips and still-unclaimed payments
    // within a day of the new batch's own timestamps. Approximate/Confident/Cancelled
    // trips are deliberately excluded — reopening an already-settled match would mean
    // unwinding its Earnings/MatchedPaymentTripUuid mutations for a rare payoff. Must run
    // on the same AppDbContext/transaction as the AddRange+SaveChanges that persisted
    // newTrips/newPayments — EF's change tracking then returns those exact tracked
    // instances alongside any older open rows in one query, no manual list merging needed.
    public static async Task<(List<Trip> CandidateTrips, List<TripPayment> CandidatePayments)> LoadCandidatesAsync(
        AppDbContext db, int userId, List<Trip> newTrips, List<TripPayment> newPayments)
    {
        if (newTrips.Count == 0 && newPayments.Count == 0)
        {
            return (newTrips, newPayments);
        }

        var anchorTimes = newTrips.Select(t => t.RequestedTimeUtc ?? t.StartTimeUtc)
            .Concat(newPayments.Select(p => p.LocalTimestamp))
            .ToList();

        // A day of padding is generous next to the 30-minute match tolerance below, and
        // keeps the filter sargable against the indexed StartTimeUtc/LocalTimestamp
        // columns (filtering on COALESCE(RequestedTimeUtc, StartTimeUtc) wouldn't be).
        var windowStart = anchorTimes.Min().AddDays(-1);
        var windowEnd = anchorTimes.Max().AddDays(1);

        var candidateTrips = await db.Trips
            .Where(t => t.UserId == userId && t.EarningsMatchQuality == PaymentMatchQuality.Unmatched
                && t.StartTimeUtc >= windowStart && t.StartTimeUtc <= windowEnd)
            .ToListAsync();

        var candidatePayments = await db.TripPayments
            .Where(p => p.UserId == userId && p.MatchedTripId == null
                && p.LocalTimestamp >= windowStart && p.LocalTimestamp <= windowEnd)
            .ToListAsync();

        return (candidateTrips, candidatePayments);
    }

    // Payments' "Local Timestamp" (despite the name) lines up with Trips' UTC request
    // time almost exactly, not any local/dropoff time — confirmed against a full real
    // import (2354 trips): comparing against RequestedTimeUtc with these thresholds
    // produced a 1.2-minute average delta and 97% match rate, versus 44% when the
    // matcher compared against dropoff time.
    private const double ConfidentThresholdMinutes = 2;
    private const double MaxToleranceMinutes = 30;

    public static TripPaymentMatchStatistics Match(List<Trip> trips, List<TripPayment> payments)
    {
        var paymentGroups = payments
            .GroupBy(p => p.TripUuid)
            .Select(g => new PaymentGroup(
                g.Key,
                NormalizeCity(g.First().CityName),
                g.Sum(p => p.LocalAmount),
                g.Min(p => p.LocalTimestamp),
                g.ToList()))
            .ToList();

        var tripsByCity = trips.GroupBy(t => NormalizeCity(t.City)).ToDictionary(g => g.Key, g => g.ToList());
        var groupsByCity = paymentGroups.GroupBy(g => g.City).ToDictionary(g => g.Key, g => g.ToList());

        var matchedTrips = new HashSet<Trip>();
        var matchedGroups = new HashSet<PaymentGroup>();

        foreach (var city in tripsByCity.Keys.Union(groupsByCity.Keys))
        {
            var cityTrips = tripsByCity.GetValueOrDefault(city, []);
            var cityGroups = groupsByCity.GetValueOrDefault(city, []);

            var candidates = new List<(Trip Trip, PaymentGroup Group, double DeltaMinutes)>();
            foreach (var trip in cityTrips)
            {
                foreach (var group in cityGroups)
                {
                    var tripAnchor = trip.RequestedTimeUtc ?? trip.StartTimeUtc;
                    var delta = Math.Abs((group.AnchorTime - tripAnchor).TotalMinutes);
                    if (delta <= MaxToleranceMinutes)
                    {
                        candidates.Add((trip, group, delta));
                    }
                }
            }

            // Globally-best-first within the city so a later trip can't steal a
            // worse-but-in-tolerance match after a better pair was already claimed.
            foreach (var candidate in candidates.OrderBy(c => c.DeltaMinutes).ThenBy(c => c.Trip.Id))
            {
                if (matchedTrips.Contains(candidate.Trip) || matchedGroups.Contains(candidate.Group))
                {
                    continue;
                }

                candidate.Trip.Earnings = candidate.Group.NetAmount;
                candidate.Trip.MatchedPaymentTripUuid = candidate.Group.TripUuid;
                candidate.Trip.EarningsMatchDeltaMinutes = candidate.DeltaMinutes;
                candidate.Trip.EarningsMatchQuality = candidate.DeltaMinutes <= ConfidentThresholdMinutes && city != "unknown"
                    ? PaymentMatchQuality.Confident
                    : PaymentMatchQuality.Approximate;

                foreach (var row in candidate.Group.Rows)
                {
                    row.MatchedTrip = candidate.Trip;
                }

                matchedTrips.Add(candidate.Trip);
                matchedGroups.Add(candidate.Group);
            }
        }

        // Earnings is non-nullable, so unmatched trips need an explicit value. Real data
        // confirms unmatched trips are cancellations where no payment was ever generated
        // (see Trip.Status) — 0 is the correct earnings, not a placeholder for "unknown".
        // An unmatched *completed* trip would be unexpected (none observed in real data),
        // but still defaults to 0 here rather than leaving the import blocked or guessing.
        foreach (var trip in trips.Where(t => t.EarningsMatchQuality == PaymentMatchQuality.Unmatched))
        {
            trip.Earnings = 0m;

            // Reclassify: a cancelled trip with no payment record is expected (no fare/fee
            // was ever charged), not a matching gap. Match on Status containing "cancel"
            // rather than the exact rider_canceled/driver_canceled strings — robust to
            // minor format variation. Genuinely unexplained non-matches stay Unmatched.
            if (trip.Status?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true)
            {
                trip.EarningsMatchQuality = PaymentMatchQuality.Cancelled;
            }
        }

        return new TripPaymentMatchStatistics(
            TripsConfidentMatch: trips.Count(t => t.EarningsMatchQuality == PaymentMatchQuality.Confident),
            TripsApproximateMatch: trips.Count(t => t.EarningsMatchQuality == PaymentMatchQuality.Approximate),
            TripsUnmatched: trips.Count(t => t.EarningsMatchQuality == PaymentMatchQuality.Unmatched),
            TripsCancelled: trips.Count(t => t.EarningsMatchQuality == PaymentMatchQuality.Cancelled),
            PaymentGroupsMatched: matchedGroups.Count,
            PaymentGroupsUnmatched: paymentGroups.Count - matchedGroups.Count);
    }

    // Payments' "City Name" has been observed as "Birmingham, AL" while Trips' city_name
    // may just be "Birmingham" — take only the part before a comma so a state/country
    // suffix on one side doesn't prevent an otherwise-correct city match.
    private static string NormalizeCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return "unknown";
        }

        var cityOnly = city.Split(',')[0];
        return cityOnly.Trim().ToLowerInvariant();
    }

    private sealed record PaymentGroup(string TripUuid, string City, decimal NetAmount, DateTime AnchorTime, List<TripPayment> Rows);
}
