using CsvHelper;
using UberGenius.Api.Data;
using UberGenius.Api.Imports;

namespace UberGenius.Api.AppAnalytics;

// Raw device/GPS telemetry, not keyed to any trip. Stored now with no join/processing
// logic so future map/GPS or online-time features don't require re-importing old
// exports. Only fields with plausible future use are kept — device/carrier metadata
// is dropped entirely (see AppAnalyticsEvent).
public static class AppAnalyticsCsvImporter
{
    private static readonly Dictionary<string, string[]> FieldAliases = new()
    {
        ["EventName"] = ["analytics event name", "event name"],
        ["EventType"] = ["analytics event type", "event type"],
        ["City"] = ["city"],
        ["IsDriverOnline"] = ["is driver online?", "is driver online"],
        ["DriverStatus"] = ["driver status"],
        ["EventTimeUtc"] = ["event time (utc)", "event time utc", "event time"],
        ["Latitude"] = ["latitude"],
        ["Longitude"] = ["longitude"],
        ["SpeedGps"] = ["speed (gps)", "speed gps", "speed"],
    };

    private static readonly string[] RequiredFields = ["EventName", "EventTimeUtc"];

    public static (CsvImportResult Result, List<AppAnalyticsEvent> Events) Import(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CsvHeaderMapper.CreateConfig());

        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
        {
            return (CsvImportResult.Failure("The file appears to be empty or has no header row.", []), []);
        }

        var headers = csv.HeaderRecord;
        var mapping = CsvHeaderMapper.MapFields(headers, FieldAliases);

        var missingRequired = RequiredFields.Where(f => !mapping.ContainsKey(f)).ToArray();
        if (missingRequired.Length > 0)
        {
            return (CsvImportResult.Failure(
                $"Could not find a matching column for: {string.Join(", ", missingRequired)}.",
                headers,
                mapping), []);
        }

        var events = new List<AppAnalyticsEvent>();
        var errors = new List<string>();
        var rowNumber = 1; // the header is row 1

        while (csv.Read())
        {
            rowNumber++;
            try
            {
                events.Add(BuildEvent(csv, mapping));
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNumber}: {ex.Message}");
            }
        }

        return (CsvImportResult.Success(headers, mapping, rowNumber - 1, events.Count, errors), events);
    }

    private static AppAnalyticsEvent BuildEvent(CsvReader csv, Dictionary<string, string> mapping)
    {
        var eventName = csv.GetField(mapping["EventName"]);
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new FormatException("Event name is missing.");
        }

        var eventTimeUtc = CsvHeaderMapper.ParseDateTime(csv.GetField(mapping["EventTimeUtc"]), "event time (UTC)");

        return new AppAnalyticsEvent
        {
            EventName = eventName,
            EventTimeUtc = eventTimeUtc,
            EventType = GetOptional(csv, mapping, "EventType"),
            City = GetOptional(csv, mapping, "City"),
            DriverStatus = GetOptional(csv, mapping, "DriverStatus"),
            IsDriverOnline = CsvHeaderMapper.TryParseBool(GetOptional(csv, mapping, "IsDriverOnline")),
            Latitude = CsvHeaderMapper.TryParseDouble(GetOptional(csv, mapping, "Latitude")),
            Longitude = CsvHeaderMapper.TryParseDouble(GetOptional(csv, mapping, "Longitude")),
            SpeedGps = CsvHeaderMapper.TryParseDouble(GetOptional(csv, mapping, "SpeedGps")),
        };
    }

    private static string? GetOptional(CsvReader csv, Dictionary<string, string> mapping, string field) =>
        mapping.TryGetValue(field, out var header) ? csv.GetField(header) : null;
}
