using Microsoft.EntityFrameworkCore;

namespace UberGenius.Api.Data;

public enum PaymentMatchQuality
{
    Unmatched = 0,
    Approximate = 1,
    Confident = 2,
    // A trip that never matched a payment, but whose Status indicates it was cancelled —
    // expected (no fare/fee was ever charged), not a matching gap. Distinct from Unmatched
    // so that bucket stays a clean signal for genuinely unexplained non-matches.
    Cancelled = 3,
}

public class Trip
{
    public int Id { get; set; }

    // All times are stored in UTC; convert to local only for display. RequestedTimeUtc is
    // null only if the file has no request-time column at all — every real row should have one.
    public DateTime? RequestedTimeUtc { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }

    public string? City { get; set; }
    public string? Status { get; set; }

    public string PickupLocation { get; set; } = string.Empty;
    public string DropoffLocation { get; set; } = string.Empty;

    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
    public double? DropoffLatitude { get; set; }
    public double? DropoffLongitude { get; set; }

    [Precision(9, 2)]
    public decimal DistanceMiles { get; set; }

    // Confirmed against real data (2346 trips) to sometimes differ from DistanceMiles
    // by rounding (±0.01 mi) — kept as a separate column deliberately.
    [Precision(9, 2)]
    public decimal? FareDistanceMiles { get; set; }

    // Real net earnings: the matched Payments total, or 0 when unmatched (confirmed against
    // real data that unmatched trips are cancellations with no payment ever generated — 0 is
    // a real value here, not a placeholder for "unknown"). See TripPaymentMatcher.
    [Precision(9, 2)]
    public decimal Earnings { get; set; }

    public string? MatchedPaymentTripUuid { get; set; }
    public PaymentMatchQuality EarningsMatchQuality { get; set; } = PaymentMatchQuality.Unmatched;
    public double? EarningsMatchDeltaMinutes { get; set; }

    public List<TripPayment> Payments { get; set; } = [];
}