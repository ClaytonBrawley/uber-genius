using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberGenius.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateCancellationFeeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationFeeLocal",
                table: "Trips");

            migrationBuilder.RenameColumn(
                name: "CancellationFeeUsd",
                table: "Trips",
                newName: "CancellationFee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CancellationFee",
                table: "Trips",
                newName: "CancellationFeeUsd");

            migrationBuilder.AddColumn<decimal>(
                name: "CancellationFeeLocal",
                table: "Trips",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);
        }
    }
}
