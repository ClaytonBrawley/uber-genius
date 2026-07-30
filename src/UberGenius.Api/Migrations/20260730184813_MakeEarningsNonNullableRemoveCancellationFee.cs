using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberGenius.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeEarningsNonNullableRemoveCancellationFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationFee",
                table: "Trips");

            // Backfill existing NULLs before enforcing NOT NULL — ALTER COLUMN's defaultValue
            // only applies to future rows, it doesn't retroactively fill existing ones.
            migrationBuilder.Sql("UPDATE [Trips] SET [Earnings] = 0 WHERE [Earnings] IS NULL;");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<decimal>(
                name: "CancellationFee",
                table: "Trips",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);
        }
    }
}
