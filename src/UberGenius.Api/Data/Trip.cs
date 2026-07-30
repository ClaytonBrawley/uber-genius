using Microsoft.EntityFrameworkCore;

namespace UberGenius.Api.Data;

public enum PaymentMatchQuality
{
    Unmatched = 0,
    Approximate = 1,
    Confident = 2,
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

    // Gross fee charged to the rider — not the driver's net payout. Confirmed identical
    // to cancellation_fee_local across all real rows, so only one column is kept.
    [Precision(9, 2)]
    public decimal? CancellationFee { get; set; }

    [Precision(9, 2)]
    public decimal? Earnings { get; set; }

    public string? MatchedPaymentTripUuid { get; set; }
    public PaymentMatchQuality EarningsMatchQuality { get; set; } = PaymentMatchQuality.Unmatched;
    public double? EarningsMatchDeltaMinutes { get; set; }

    public List<TripPayment> Payments { get; set; } = [];
}