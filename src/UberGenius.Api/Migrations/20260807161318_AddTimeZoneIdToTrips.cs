using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberGenius.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeZoneIdToTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing accounts (the owner's real one plus a handful of test/demo signups)
            // predate this column and have only ever meant Central time — a real, valid
            // default rather than an empty string, which TimeZoneInfo would reject outright.
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "America/Chicago");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Trips");
        }
    }
}
