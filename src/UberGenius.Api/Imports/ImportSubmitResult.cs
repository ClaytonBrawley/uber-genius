namespace UberGenius.Api.Imports;

public record ImportSubmitResult(
    bool Succeeded,
    string? Error,
    CsvImportResult DriverProfileResult,
    CsvImportResult TripsResult,
    CsvImportResult PaymentsResult,
    CsvImportResult AppAnalyticsResult,
    TripPaymentMatchStatistics? MatchStatistics,
    int TripsImported,
    int PaymentRowsImported,
    int AppAnalyticsEventsImported);
