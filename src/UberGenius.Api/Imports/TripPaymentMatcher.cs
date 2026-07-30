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
// match, not a key join. Scoped to the current submit batch only (new Trips against
// new Payments from the same submission) — matching against previously-persisted rows
// is explicit future work.
public static class TripPaymentMatcher
{
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
