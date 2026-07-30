using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

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

    // Payments has no UTC timestamp, only a local one, so the approximate join needs a
    // like-for-like local time. Not persisted — recomputed from the CSV at import time,
    // used only in-memory by TripPaymentMatcher.
    [NotMapped]
    public DateTime? EndTimeLocalForMatching { get; set; }

    public string? City { get; set; }

    public string PickupLocation { get; set; } = string.Empty;
    public string DropoffLocation { get; set; } = string.Empty;

    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
    public double? DropoffLatitude { get; set; }
    public double? DropoffLongitude { get; set; }

    [Precision(9, 2)]
    public decimal DistanceMiles { get; set; }

    [Precision(9, 2)]
    public decimal? Earnings { get; set; }

    public string? MatchedPaymentTripUuid { get; set; }
    public PaymentMatchQuality EarningsMatchQuality { get; set; } = PaymentMatchQuality.Unmatched;
    public double? EarningsMatchDeltaMinutes { get; set; }

    public List<TripPayment> Payments { get; set; } = [];
}