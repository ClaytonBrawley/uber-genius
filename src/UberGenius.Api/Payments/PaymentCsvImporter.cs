using CsvHelper;
using UberGenius.Api.Data;
using UberGenius.Api.Imports;

namespace UberGenius.Api.Payments;

// Each trip has multiple payment line items (fare components, commission, incentives,
// adjustments). Net earnings per trip = sum of every row's LocalAmount grouped by
// TripUuid — confirmed against real data, no Classification/Category filtering needed.
public static class PaymentCsvImporter
{
    private static readonly Dictionary<string, string[]> FieldAliases = new()
    {
        ["TripUuid"] = ["trip uuid", "tripuuid"],
        ["CityName"] = ["city name", "cityname"],
        ["LocalAmount"] = ["local amount", "localamount"],
        ["CurrencyCode"] = ["currency code", "currencycode"],
        ["Classification"] = ["classification"],
        ["Category"] = ["category"],
        ["LocalTimestamp"] = ["local timestamp", "localtimestamp"],
    };

    private static readonly string[] RequiredFields = ["TripUuid", "LocalAmount", "LocalTimestamp"];

    public static (CsvImportResult Result, List<TripPayment> Payments) Import(Stream csvStream)
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

        var payments = new List<TripPayment>();
        var errors = new List<string>();
        var rowNumber = 1; // the header is row 1

        while (csv.Read())
        {
            rowNumber++;
            try
            {
                payments.Add(BuildPayment(csv, mapping));
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNumber}: {ex.Message}");
            }
        }

        return (CsvImportResult.Success(headers, mapping, rowNumber - 1, payments.Count, errors), payments);
    }

    private static TripPayment BuildPayment(CsvReader csv, Dictionary<string, string> mapping)
    {
        var tripUuid = csv.GetField(mapping["TripUuid"]);
        if (string.IsNullOrWhiteSpace(tripUuid))
        {
            throw new FormatException("Trip UUID is missing.");
        }

        var localAmount = CsvHeaderMapper.ParseDecimal(csv.GetField(mapping["LocalAmount"]), "local amount");
        var localTimestamp = CsvHeaderMapper.ParseDateTime(csv.GetField(mapping["LocalTimestamp"]), "local timestamp");

        return new TripPayment
        {
            TripUuid = tripUuid,
            LocalAmount = localAmount,
            LocalTimestamp = localTimestamp,
            CityName = mapping.TryGetValue("CityName", out var cityHeader) ? csv.GetField(cityHeader) ?? "" : "",
            CurrencyCode = mapping.TryGetValue("CurrencyCode", out var currencyHeader) ? csv.GetField(currencyHeader) ?? "" : "",
            Classification = mapping.TryGetValue("Classification", out var classificationHeader) ? csv.GetField(classificationHeader) ?? "" : "",
            Category = mapping.TryGetValue("Category", out var categoryHeader) ? csv.GetField(categoryHeader) ?? "" : "",
        };
    }
}
