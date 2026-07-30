using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberGenius.Api.Migrations
{
    /// <inheritdoc />
    public partial class StoreTripTimesAsUtcWithRequestedTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Trips",
                newName: "StartTimeUtc");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "Trips",
                newName: "EndTimeUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedTimeUtc",
                table: "Trips",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedTimeUtc",
                table: "Trips");

            migrationBuilder.RenameColumn(
                name: "StartTimeUtc",
                table: "Trips",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "EndTimeUtc",
                table: "Trips",
                newName: "EndTime");
        }
    }
}
