using System.Security.Claims;
using UberGenius.Api.AppAnalytics;
using UberGenius.Api.Auth;
using UberGenius.Api.Data;
using UberGenius.Api.DriverProfiles;
using UberGenius.Api.Payments;
using UberGenius.Api.Trips;

namespace UberGenius.Api.Imports;

public static class ImportEndpoints
{
    public static void MapImportEndpoints(this WebApplication app)
    {
        app.MapPost("/api/imports/validate/{category}", async (string category, IFormFile file) =>
        {
            var result = category switch
            {
                "driver-profile" => (await ParseAsync(file, DriverProfileCsvImporter.Import)).Result,
                "trips" => (await ParseAsync(file, TripCsvImporter.Import)).Result,
                "payments" => (await ParseAsync(file, PaymentCsvImporter.Import)).Result,
                "app-analytics" => (await ParseAsync(file, AppAnalyticsCsvImporter.Import)).Result,
                _ => CsvImportResult.Failure($"Unknown import category '{category}'.", []),
            };

            return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
        }).DisableAntiforgery().RequireAuthorization();

        app.MapPost("/api/imports/submit", async (
            IFormFile driverProfile,
            IFormFile trips,
            IFormFile payments,
            IFormFile appAnalytics,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            // Files are re-parsed here regardless of any earlier client-side validation —
            // that step is UX feedback, not the integrity boundary.
            var (driverProfileResult, driverProfiles) = await ParseAsync(driverProfile, DriverProfileCsvImporter.Import);
            var (tripsResult, tripList) = await ParseAsync(trips, TripCsvImporter.Import);
            var (paymentsResult, paymentList) = await ParseAsync(payments, PaymentCsvImporter.Import);
            var (appAnalyticsResult, analyticsEvents) = await ParseAsync(appAnalytics, AppAnalyticsCsvImporter.Import);

            if (!driverProfileResult.Succeeded || !tripsResult.Succeeded || !paymentsResult.Succeeded || !appAnalyticsResult.Succeeded)
            {
                return Results.BadRequest(new ImportSubmitResult(
                    false,
                    "One or more files failed validation. No data was saved.",
                    driverProfileResult, tripsResult, paymentsResult, appAnalyticsResult,
                    null, 0, 0, 0));
            }

            var userId = principal.GetUserId();
            driverProfiles.StampUserId(userId);
            analyticsEvents.StampUserId(userId);
            paymentList.StampUserId(userId);
            tripList.StampUserId(userId);

            await using var transaction = await db.Database.BeginTransactionAsync();

            db.DriverProfiles.AddRange(driverProfiles);
            db.AppAnalyticsEvents.AddRange(analyticsEvents);
            db.TripPayments.AddRange(paymentList);
            db.Trips.AddRange(tripList);
            await db.SaveChangesAsync(); // assigns Ids needed for tie-breaking in the matcher

            var matchStatistics = TripPaymentMatcher.Match(tripList, paymentList);
            await db.SaveChangesAsync(); // persists Earnings/match-quality updates from the matcher

            await transaction.CommitAsync();

            return Results.Ok(new ImportSubmitResult(
                true,
                null,
                driverProfileResult, tripsResult, paymentsResult, appAnalyticsResult,
                matchStatistics,
                tripList.Count,
                paymentList.Count,
                analyticsEvents.Count));
        }).DisableAntiforgery().RequireAuthorization();
    }

    // Wraps parsing so any unexpected failure (bad encoding, malformed file, etc.) comes
    // back as a diagnosable CsvImportResult instead of an opaque 500 with no explanation.
    private static async Task<(CsvImportResult Result, List<T> Items)> ParseAsync<T>(
        IFormFile file,
        Func<Stream, (CsvImportResult Result, List<T> Items)> import)
    {
        if (file.Length == 0)
        {
            return (CsvImportResult.Failure("No file was uploaded.", []), []);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            return import(stream);
        }
        catch (Exception ex)
        {
            return (CsvImportResult.Failure($"Unexpected error while parsing the file: {ex.Message}", []), []);
        }
    }
}
