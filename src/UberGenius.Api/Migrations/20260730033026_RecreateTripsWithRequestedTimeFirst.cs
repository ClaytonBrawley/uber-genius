using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberGenius.Api.Migrations
{
    /// <inheritdoc />
    public partial class RecreateTripsWithRequestedTimeFirst : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reordering physical columns requires recreating the table — clearing all
            // data at the same time per an explicit request to reset the (test-only)
            // dataset now that the Payments-matching fix changes what "correct" looks like.
            migrationBuilder.Sql("DELETE FROM [TripPayments];");
            migrationBuilder.Sql("DELETE FROM [AppAnalyticsEvents];");
            migrationBuilder.Sql("DELETE FROM [DriverProfiles];");

            migrationBuilder.Sql("ALTER TABLE [TripPayments] DROP CONSTRAINT [FK_TripPayments_Trips_MatchedTripId];");
            migrationBuilder.Sql("DROP TABLE [Trips];");

            migrationBuilder.Sql("""
                CREATE TABLE [Trips] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [RequestedTimeUtc] datetime2 NULL,
                    [StartTimeUtc] datetime2 NOT NULL,
                    [EndTimeUtc] datetime2 NOT NULL,
                    [City] nvarchar(max) NULL,
                    [PickupLocation] nvarchar(max) NOT NULL,
                    [DropoffLocation] nvarchar(max) NOT NULL,
                    [PickupLatitude] float NULL,
                    [PickupLongitude] float NULL,
                    [DropoffLatitude] float NULL,
                    [DropoffLongitude] float NULL,
                    [DistanceMiles] decimal(9,2) NOT NULL,
                    [Earnings] decimal(9,2) NULL,
                    [MatchedPaymentTripUuid] nvarchar(max) NULL,
                    [EarningsMatchQuality] nvarchar(max) NOT NULL,
                    [EarningsMatchDeltaMinutes] float NULL,
                    CONSTRAINT [PK_Trips] PRIMARY KEY ([Id])
                );
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [TripPayments] ADD CONSTRAINT [FK_TripPayments_Trips_MatchedTripId]
                    FOREIGN KEY ([MatchedTripId]) REFERENCES [Trips] ([Id]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "This migration recreates Trips destructively (column reorder + data reset); there is no meaningful down migration.");
        }
    }
}
