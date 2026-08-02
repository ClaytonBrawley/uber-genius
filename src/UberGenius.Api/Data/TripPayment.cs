using Microsoft.EntityFrameworkCore;

namespace UberGenius.Api.Data;

public class TripPayment : IUserOwned
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string TripUuid { get; set; } = "";
    public string CityName { get; set; } = "";

    [Precision(9, 2)]
    public decimal LocalAmount { get; set; }

    public string CurrencyCode { get; set; } = "";
    public string Classification { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime LocalTimestamp { get; set; }

    public int? MatchedTripId { get; set; }
    public Trip? MatchedTrip { get; set; }
}
