using Microsoft.EntityFrameworkCore;

namespace UberGenius.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripPayment> TripPayments => Set<TripPayment>();
    public DbSet<AppAnalyticsEvent> AppAnalyticsEvents => Set<AppAnalyticsEvent>();
    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trip>()
            .Property(t => t.EarningsMatchQuality)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Trip>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Trip>()
            .HasIndex(t => new { t.UserId, t.StartTimeUtc });

        modelBuilder.Entity<TripPayment>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TripPayment>()
            .HasIndex(p => new { p.UserId, p.LocalTimestamp });

        modelBuilder.Entity<AppAnalyticsEvent>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AppAnalyticsEvent>()
            .HasIndex(e => new { e.UserId, e.EventTimeUtc });

        modelBuilder.Entity<DriverProfile>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DriverProfile>()
            .HasIndex(d => d.UserId);
    }
}