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

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

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