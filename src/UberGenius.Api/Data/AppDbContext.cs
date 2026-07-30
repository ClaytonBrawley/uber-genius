using Microsoft.EntityFrameworkCore;

namespace UberGenius.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripPayment> TripPayments => Set<TripPayment>();
    public DbSet<AppAnalyticsEvent> AppAnalyticsEvents => Set<AppAnalyticsEvent>();
    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trip>()
            .Property(t => t.EarningsMatchQuality)
            .HasConversion<string>();
    }
}