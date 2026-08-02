using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberGenius.Api.Migrations
{
    /// <inheritdoc />
    public partial class BackfillOwnerDataAndEnforceUserIdRequired : Migration
    {
        // Backfills any pre-existing, still-unowned rows (from before Users existed) to the
        // owner's own account, then locks UserId down to required. On a fresh/empty database
        // the UPDATE statements simply match zero rows, so this is safe to replay from scratch.
        private const string OwnerEmail = "claytonbrawley@gmail.com";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"UPDATE [Trips] SET [UserId] = (SELECT [Id] FROM [Users] WHERE [Email] = '{OwnerEmail}') WHERE [UserId] IS NULL;");
            migrationBuilder.Sql($"UPDATE [TripPayments] SET [UserId] = (SELECT [Id] FROM [Users] WHERE [Email] = '{OwnerEmail}') WHERE [UserId] IS NULL;");
            migrationBuilder.Sql($"UPDATE [AppAnalyticsEvents] SET [UserId] = (SELECT [Id] FROM [Users] WHERE [Email] = '{OwnerEmail}') WHERE [UserId] IS NULL;");
            migrationBuilder.Sql($"UPDATE [DriverProfiles] SET [UserId] = (SELECT [Id] FROM [Users] WHERE [Email] = '{OwnerEmail}') WHERE [UserId] IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Trips",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "TripPayments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "AppAnalyticsEvents",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "DriverProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Trips",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "TripPayments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "AppAnalyticsEvents",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "DriverProfiles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
