using Microsoft.EntityFrameworkCore;
using UberGenius.Api.Data;

namespace UberGenius.Api.Imports;

public record DedupeResult<T>(List<T> NewItems, int SkippedCount);

// None of the 4 Uber export CSVs carry a real unique row ID, so "already imported" has to
// be decided from a natural key built out of the columns that do exist. Each key was chosen
// by checking what's actually stable across two exports of the same underlying data — see
// the comments on each method. DriverProfile is deliberately absent here: it's an
// intentional append-only snapshot-per-import table (see DriverProfile.cs), never deduped.
public static class ImportDeduper
{
    public static async Task<DedupeResult<Trip>> DedupeTripsAsync(AppDbContext db, int userId, List<Trip> incoming)
    {
        if (incoming.Count == 0)
        {
            return new DedupeResult<Trip>(incoming, 0);
        }

        var windowStart = incoming.Min(t => t.StartTimeUtc);
        var windowEnd = incoming.Max(t => t.StartTimeUtc);

        // EndTimeUtc is deliberately not part of the key: TripCsvImporter falls back to
        // StartTimeUtc when the CSV's dropoff column is blank, so the same real trip's
        // EndTimeUtc isn't guaranteed identical across two exports. StartTimeUtc alone is
        // safe — one driver can't begin two different trips at the same instant.
        var existingKeys = await db.Trips.AsNoTracking()
            .Where(t => t.UserId == userId && t.StartTimeUtc >= windowStart && t.StartTimeUtc <= windowEnd)
            .Select(t => t.StartTimeUtc)
            .ToListAsync();

        return Filter(incoming, t => t.StartTimeUtc, existingKeys);
    }

    public static async Task<DedupeResult<TripPayment>> DedupePaymentsAsync(AppDbContext db, int userId, List<TripPayment> incoming)
    {
        if (incoming.Count == 0)
        {
            return new DedupeResult<TripPayment>(incoming, 0);
        }

        var windowStart = incoming.Min(p => p.LocalTimestamp);
        var windowEnd = incoming.Max(p => p.LocalTimestamp);

        // CityName is deliberately excluded — TripPaymentMatcher.NormalizeCity's comment
        // notes it's been observed inconsistently formatted ("Birmingham, AL" vs
        // "Birmingham"), so it isn't safe as an exact-equality key component.
        var existingKeys = await db.TripPayments.AsNoTracking()
            .Where(p => p.UserId == userId && p.LocalTimestamp >= windowStart && p.LocalTimestamp <= windowEnd)
            .Select(p => new PaymentKey(p.TripUuid, p.Classification, p.Category, p.LocalAmount, p.LocalTimestamp))
            .ToListAsync();

        return Filter(incoming, PaymentKey.From, existingKeys);
    }

    public static async Task<DedupeResult<AppAnalyticsEvent>> DedupeAnalyticsEventsAsync(AppDbContext db, int userId, List<AppAnalyticsEvent> incoming)
    {
        if (incoming.Count == 0)
        {
            return new DedupeResult<AppAnalyticsEvent>(incoming, 0);
        }

        var windowStart = incoming.Min(e => e.EventTimeUtc);
        var windowEnd = incoming.Max(e => e.EventTimeUtc);

        // No ID column exists and the schema anticipates multiple event kinds sharing a
        // timestamp, so the key uses every mapped field, not just EventTimeUtc.
        var existingKeys = await db.AppAnalyticsEvents.AsNoTracking()
            .Where(e => e.UserId == userId && e.EventTimeUtc >= windowStart && e.EventTimeUtc <= windowEnd)
            .Select(e => new AnalyticsEventKey(e.EventTimeUtc, e.EventName, e.EventType, e.City,
                e.IsDriverOnline, e.DriverStatus, e.Latitude, e.Longitude, e.SpeedGps))
            .ToListAsync();

        return Filter(incoming, AnalyticsEventKey.From, existingKeys);
    }

    // Filtering happens in C#, not translated to SQL, because several key columns are
    // nullable and SQL's NULL = NULL is UNKNOWN, not TRUE — an EF-translated .Contains()
    // over a tuple set would silently under-dedupe AppAnalyticsEvents. Record equality
    // handles null == null correctly by construction. Also rejects a key seen twice within
    // the same incoming file, not just against the DB — otherwise two literal duplicate
    // lines in one CSV would both pass the DB-existence check and both get inserted.
    private static DedupeResult<T> Filter<T, TKey>(List<T> incoming, Func<T, TKey> keySelector, List<TKey> existingKeys)
        where TKey : notnull
    {
        var existingSet = existingKeys.ToHashSet();
        var seenInBatch = new HashSet<TKey>();
        var newItems = new List<T>(incoming.Count);

        foreach (var item in incoming)
        {
            var key = keySelector(item);
            if (existingSet.Contains(key) || !seenInBatch.Add(key))
            {
                continue;
            }
            newItems.Add(item);
        }

        return new DedupeResult<T>(newItems, incoming.Count - newItems.Count);
    }

    private sealed record PaymentKey(string TripUuid, string Classification, string Category, decimal LocalAmount, DateTime LocalTimestamp)
    {
        public static PaymentKey From(TripPayment p) => new(p.TripUuid, p.Classification, p.Category, p.LocalAmount, p.LocalTimestamp);
    }

    private sealed record AnalyticsEventKey(
        DateTime EventTimeUtc, string EventName, string? EventType, string? City,
        bool? IsDriverOnline, string? DriverStatus, double? Latitude, double? Longitude, double? SpeedGps)
    {
        public static AnalyticsEventKey From(AppAnalyticsEvent e) =>
            new(e.EventTimeUtc, e.EventName, e.EventType, e.City, e.IsDriverOnline, e.DriverStatus, e.Latitude, e.Longitude, e.SpeedGps);
    }
}
