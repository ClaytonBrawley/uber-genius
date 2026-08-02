using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberGenius.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndNullableUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Trips",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "TripPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "DriverProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "AppAnalyticsEvents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_UserId_StartTimeUtc",
                table: "Trips",
                columns: new[] { "UserId", "StartTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TripPayments_UserId",
                table: "TripPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_UserId",
                table: "DriverProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppAnalyticsEvents_UserId_EventTimeUtc",
                table: "AppAnalyticsEvents",
                columns: new[] { "UserId", "EventTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AppAnalyticsEvents_Users_UserId",
                table: "AppAnalyticsEvents",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverProfiles_Users_UserId",
                table: "DriverProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TripPayments_Users_UserId",
                table: "TripPayments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Users_UserId",
                table: "Trips",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppAnalyticsEvents_Users_UserId",
                table: "AppAnalyticsEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_DriverProfiles_Users_UserId",
                table: "DriverProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_TripPayments_Users_UserId",
                table: "TripPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Users_UserId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Trips_UserId_StartTimeUtc",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_TripPayments_UserId",
                table: "TripPayments");

            migrationBuilder.DropIndex(
                name: "IX_DriverProfiles_UserId",
                table: "DriverProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AppAnalyticsEvents_UserId_EventTimeUtc",
                table: "AppAnalyticsEvents");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TripPayments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AppAnalyticsEvents");
        }
    }
}
