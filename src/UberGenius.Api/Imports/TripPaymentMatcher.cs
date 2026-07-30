using UberGenius.Api.Data;

namespace UberGenius.Api.Imports;

public record TripPaymentMatchStatistics(
    int TripsConfidentMatch,
    int TripsApproximateMatch,
    int TripsUnmatched,
    int PaymentGroupsMatched,
    int PaymentGroupsUnmatched);

// Trips have no unique ID and Payments have no reference back to Trips other than
// city + a per-trip-group timestamp, so this is a best-effort greedy nearest-neighbor
// match, not a key join. Scoped to the current submit batch only (new Trips against
// new Payments from the same submission) — matching against previously-persisted rows
// is explicit future work.
public static class TripPaymentMatcher
{
    private const double ConfidentThresholdMinutes = 10;
    private const double MaxToleranceMinutes = 180;

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
                    // Payments only has a local timestamp, so the comparison needs a
                    // like-for-like local anchor on the Trip side, not the canonical UTC time.
                    var tripAnchor = trip.EndTimeLocalForMatching ?? trip.EndTimeUtc;
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

        return new TripPaymentMatchStatistics(
            TripsConfidentMatch: trips.Count(t => t.EarningsMatchQuality == PaymentMatchQuality.Confident),
            TripsApproximateMatch: trips.Count(t => t.EarningsMatchQuality == PaymentMatchQuality.Approximate),
            TripsUnmatched: trips.Count(t => t.EarningsMatchQuality == PaymentMatchQuality.Unmatched),
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
