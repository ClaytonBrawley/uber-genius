using UberGenius.Api.AppAnalytics;
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
            if (file.Length == 0)
            {
                return Results.BadRequest("No file was uploaded.");
            }

            await using var stream = file.OpenReadStream();

            var result = category switch
            {
                "driver-profile" => DriverProfileCsvImporter.Import(stream).Result,
                "trips" => TripCsvImporter.Import(stream).Result,
                "payments" => PaymentCsvImporter.Import(stream).Result,
                "app-analytics" => AppAnalyticsCsvImporter.Import(stream).Result,
                _ => CsvImportResult.Failure($"Unknown import category '{category}'.", []),
            };

            return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
        }).DisableAntiforgery();
    }
}
