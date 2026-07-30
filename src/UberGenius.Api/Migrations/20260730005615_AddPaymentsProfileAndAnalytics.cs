using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberGenius.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsProfileAndAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Earnings",
                table: "Trips",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,2)",
                oldPrecision: 9,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EarningsMatchDeltaMinutes",
                table: "Trips",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EarningsMatchQuality",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MatchedPaymentTripUuid",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppAnalyticsEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDriverOnline = table.Column<bool>(type: "bit", nullable: true),
                    DriverStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    SpeedGps = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAnalyticsEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    DriversUnderPartner = table.Column<int>(type: "int", nullable: true),
                    ProfileId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActiveSince = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPartner = table.Column<bool>(type: "bit", nullable: true),
                    OptedInToEmail = table.Column<bool>(type: "bit", nullable: true),
                    OptedInToSms = table.Column<bool>(type: "bit", nullable: true),
                    LanguagePreference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredLanguageScript = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LifetimeCompletedTrips = table.Column<int>(type: "int", nullable: true),
                    OperatingCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatingCountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FleetType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferralCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignupCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignupDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TripPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripUuid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocalAmount = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocalTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MatchedTripId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripPayments_Trips_MatchedTripId",
                        column: x => x.MatchedTripId,
                        principalTable: "Trips",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripPayments_MatchedTripId",
                table: "TripPayments",
                column: "MatchedTripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppAnalyticsEvents");

            migrationBuilder.DropTable(
                name: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "TripPayments");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "EarningsMatchDeltaMinutes",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "EarningsMatchQuality",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "MatchedPaymentTripUuid",
                table: "Trips");

            migrationBuilder.AlterColumn<decimal>(
                name: "Earnings",
                table: "Trips",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,2)",
                oldPrecision: 9,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
