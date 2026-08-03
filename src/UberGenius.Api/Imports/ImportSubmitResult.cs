namespace UberGenius.Api.Imports;

// Parsed = rows read from the CSV. Added = rows that didn't already exist and were
// inserted. Skipped = rows that were either already in the database or a duplicate of
// another row within this same file (see ImportDeduper). DriverProfile is append-only —
// always Added == Parsed, Skipped == 0 — but kept in the same shape for a uniform
// frontend loop across all four categories.
public record ImportCounts(int Parsed, int Added, int Skipped);

public record ImportSubmitResult(
    bool Succeeded,
    string? Error,
    CsvImportResult DriverProfileResult,
    CsvImportResult TripsResult,
    CsvImportResult PaymentsResult,
    CsvImportResult AppAnalyticsResult,
    TripPaymentMatchStatistics? MatchStatistics,
    ImportCounts DriverProfileCounts,
    ImportCounts TripsCounts,
    ImportCounts PaymentRowsCounts,
    ImportCounts AppAnalyticsEventsCounts);
