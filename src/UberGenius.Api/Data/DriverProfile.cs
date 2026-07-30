using Microsoft.EntityFrameworkCore;

namespace UberGenius.Api.Data;

// One snapshot row per successful import — append-only rather than upserted in place,
// since Rating/LifetimeCompletedTrips change over time and a history has future value.
public class DriverProfile
{
    public int Id { get; set; }
    public DateTime ImportedAtUtc { get; set; }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? PreferredName { get; set; }
    public string PhoneNumber { get; set; } = "";
    public string Email { get; set; } = "";

    [Precision(3, 2)]
    public decimal? Rating { get; set; }

    public int? DriversUnderPartner { get; set; }
    public string? ProfileId { get; set; }
    public DateTime? ActiveSince { get; set; }
    public bool? IsPartner { get; set; }
    public bool? OptedInToEmail { get; set; }
    public bool? OptedInToSms { get; set; }
    public string? LanguagePreference { get; set; }
    public string? PreferredLanguageScript { get; set; }
    public int? LifetimeCompletedTrips { get; set; }
    public string? OperatingCity { get; set; }
    public string? OperatingCountry { get; set; }
    public string? FleetType { get; set; }
    public string? ReferralCode { get; set; }
    public string? SignupCity { get; set; }
    public DateTime? SignupDate { get; set; }
}
