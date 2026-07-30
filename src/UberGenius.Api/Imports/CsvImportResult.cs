namespace UberGenius.Api.Imports;

public record CsvImportResult(
    bool Succeeded,
    string? Error,
    string[] HeadersFound,
    Dictionary<string, string> ColumnMapping,
    int RowsRead,
    int RowsImported,
    List<string> RowErrors)
{
    public static CsvImportResult Failure(string error, string[] headers, Dictionary<string, string>? mapping = null) =>
        new(false, error, headers, mapping ?? new Dictionary<string, string>(), 0, 0, new List<string>());

    public static CsvImportResult Success(
        string[] headers,
        Dictionary<string, string> mapping,
        int rowsRead,
        int rowsImported,
        List<string> rowErrors) =>
        new(true, null, headers, mapping, rowsRead, rowsImported, rowErrors);
}
