using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberGenius.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLocalTimestampIndexToTripPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripPayments_UserId",
                table: "TripPayments");

            migrationBuilder.CreateIndex(
                name: "IX_TripPayments_UserId_LocalTimestamp",
                table: "TripPayments",
                columns: new[] { "UserId", "LocalTimestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripPayments_UserId_LocalTimestamp",
                table: "TripPayments");

            migrationBuilder.CreateIndex(
                name: "IX_TripPayments_UserId",
                table: "TripPayments",
                column: "UserId");
        }
    }
}
