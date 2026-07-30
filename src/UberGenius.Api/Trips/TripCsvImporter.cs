using CsvHelper;
using UberGenius.Api.Data;
using UberGenius.Api.Imports;

namespace UberGenius.Api.Trips;

// Trip earnings no longer come from this file — the real Uber Trips export has no
// earnings column and no unique trip ID at all. Net earnings are computed separately
// from the Payments file and joined in afterward (see TripPaymentMatcher).
public static class TripCsvImporter
{
    private static readonly Dictionary<string, string[]> FieldAliases = new()
    {
        // UTC columns are the canonical stored values (best practice: store UTC, localize
        // for display). More specific aliases listed before vaguer ones since MapFields
        // takes the first matching alias.
        ["StartTime"] = ["begintrip_timestamp_utc", "trip start time", "start time", "begin trip time", "pickup time"],
        ["EndTime"] = ["dropoff_timestamp_utc", "trip end time", "end time", "dropoff time", "trip completed"],
        ["RequestedTime"] = ["request_timestamp_utc", "trip requested", "request time"],
        ["City"] = ["city_name", "city"],
        ["Status"] = ["status"],
        ["PickupLocation"] = ["pickup address", "pickup location", "origin", "pickup"],
        ["DropoffLocation"] = ["dropoff address", "drop off address", "destination", "dropoff location", "dropoff"],
        ["DistanceMiles"] = ["trip_distance_miles", "distance mi", "distance", "trip distance", "miles"],
        ["FareDistanceMiles"] = ["fare_distance_miles"],
        ["CancellationFee"] = ["cancellation_fee_usd", "cancellation_fee_local"],
    };

    private static readonly string[] RequiredFields = ["StartTime"];

    public static (CsvImportResult Result, List<Trip> Trips) Import(Stream csvStream)
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

        var trips = new List<Trip>();
        var errors = new List<string>();
        var rowNumber = 1; // the header is row 1

        while (csv.Read())
        {
            rowNumber++;
            try
            {
                trips.Add(BuildTrip(csv, mapping));
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNumber}: {ex.Message}");
            }
        }

        return (CsvImportResult.Success(headers, mapping, rowNumber - 1, trips.Count, errors), trips);
    }

    private static Trip BuildTrip(CsvReader csv, Dictionary<string, string> mapping)
    {
        DateTime? GetTime(string field) =>
            mapping.TryGetValue(field, out var header) ? CsvHeaderMapper.TryParseDateTime(csv.GetField(header)) : null;

        var requestedTimeUtc = GetTime("RequestedTime");

        // Cancelled trips never begin, so begintrip_timestamp_utc is blank — fall back to
        // the request time as the best available anchor rather than failing the whole row.
        var startTimeUtc = GetTime("StartTime") ?? requestedTimeUtc
            ?? throw new FormatException("Could not determine a start time (begin-trip and request time are both missing).");

        var endTimeUtc = GetTime("EndTime") ?? startTimeUtc;

        var distance = mapping.TryGetValue("DistanceMiles", out var distHeader)
            ? CsvHeaderMapper.TryParseDecimal(csv.GetField(distHeader)) ?? 0m
            : 0m;

        decimal? GetDecimal(string field) =>
            mapping.TryGetValue(field, out var header) ? CsvHeaderMapper.TryParseDecimal(csv.GetField(header)) : null;

        return new Trip
        {
            RequestedTimeUtc = requestedTimeUtc,
            StartTimeUtc = startTimeUtc,
            EndTimeUtc = endTimeUtc,
            DistanceMiles = distance,
            FareDistanceMiles = GetDecimal("FareDistanceMiles"),
            CancellationFee = GetDecimal("CancellationFee"),
            City = mapping.TryGetValue("City", out var cityHeader) ? csv.GetField(cityHeader) : null,
            Status = mapping.TryGetValue("Status", out var statusHeader) ? csv.GetField(statusHeader) : null,
            PickupLocation = mapping.TryGetValue("PickupLocation", out var pickupHeader)
                ? csv.GetField(pickupHeader) ?? ""
                : "",
            DropoffLocation = mapping.TryGetValue("DropoffLocation", out var dropoffHeader)
                ? csv.GetField(dropoffHeader) ?? ""
                : "",
        };
    }
}
