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
                var emptyCounts = new ImportCounts(0, 0, 0);
                return Results.BadRequest(new ImportSubmitResult(
                    false,
                    "One or more files failed validation. No data was saved.",
                    driverProfileResult, tripsResult, paymentsResult, appAnalyticsResult,
                    null, emptyCounts, emptyCounts, emptyCounts, emptyCounts));
            }

            var userId = principal.GetUserId();
            driverProfiles.StampUserId(userId);
            analyticsEvents.StampUserId(userId);
            paymentList.StampUserId(userId);
            tripList.StampUserId(userId);

            // Dedup reads run in the same transaction as the writes below, so a concurrent
            // submit from the same user can't slip a row past the existence check.
            await using var transaction = await db.Database.BeginTransactionAsync();

            var tripDedupe = await ImportDeduper.DedupeTripsAsync(db, userId, tripList);
            var paymentDedupe = await ImportDeduper.DedupePaymentsAsync(db, userId, paymentList);
            var analyticsDedupe = await ImportDeduper.DedupeAnalyticsEventsAsync(db, userId, analyticsEvents);

            db.DriverProfiles.AddRange(driverProfiles);
            db.AppAnalyticsEvents.AddRange(analyticsDedupe.NewItems);
            db.TripPayments.AddRange(paymentDedupe.NewItems);
            db.Trips.AddRange(tripDedupe.NewItems);
            await db.SaveChangesAsync(); // assigns Ids needed for tie-breaking in the matcher

            TripPaymentMatchStatistics? matchStatistics = null;
            if (tripDedupe.NewItems.Count > 0 || paymentDedupe.NewItems.Count > 0)
            {
                var (candidateTrips, candidatePayments) =
                    await TripPaymentMatcher.LoadCandidatesAsync(db, userId, tripDedupe.NewItems, paymentDedupe.NewItems);
                matchStatistics = TripPaymentMatcher.Match(candidateTrips, candidatePayments);
                await db.SaveChangesAsync(); // persists Earnings/match-quality updates from the matcher
            }

            await transaction.CommitAsync();

            return Results.Ok(new ImportSubmitResult(
                true,
                null,
                driverProfileResult, tripsResult, paymentsResult, appAnalyticsResult,
                matchStatistics,
                new ImportCounts(driverProfiles.Count, driverProfiles.Count, 0),
                new ImportCounts(tripList.Count, tripDedupe.NewItems.Count, tripDedupe.SkippedCount),
                new ImportCounts(paymentList.Count, paymentDedupe.NewItems.Count, paymentDedupe.SkippedCount),
                new ImportCounts(analyticsEvents.Count, analyticsDedupe.NewItems.Count, analyticsDedupe.SkippedCount)));
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
