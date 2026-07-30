using CsvHelper;
using UberGenius.Api.Data;
using UberGenius.Api.Imports;

namespace UberGenius.Api.DriverProfiles;

// One row per driver. All columns are persisted (including PII) since there's no
// reason yet to assume they won't be useful for a future profile feature.
public static class DriverProfileCsvImporter
{
    private static readonly Dictionary<string, string[]> FieldAliases = new()
    {
        ["FirstName"] = ["first name"],
        ["LastName"] = ["last name"],
        ["PreferredName"] = ["preferred name"],
        ["PhoneNumber"] = ["phone number"],
        ["Email"] = ["e-mail", "email"],
        ["Rating"] = ["rating"],
        ["DriversUnderPartner"] = ["drivers under partner"],
        ["ProfileId"] = ["driver profile"],
        ["ActiveSince"] = ["active since"],
        ["IsPartner"] = ["is partner"],
        ["OptedInToEmail"] = ["opted-in to e-mail", "opted-in to email"],
        ["OptedInToSms"] = ["opted-in to sms"],
        ["LanguagePreference"] = ["language preference"],
        ["PreferredLanguageScript"] = ["preferred language script"],
        ["LifetimeCompletedTrips"] = ["lifetime completed trips"],
        ["OperatingCity"] = ["operating city"],
        ["OperatingCountry"] = ["operating country"],
        ["FleetType"] = ["fleet type"],
        ["ReferralCode"] = ["referral code"],
        ["SignupCity"] = ["signup city"],
        ["SignupDate"] = ["signup date"],
    };

    private static readonly string[] RequiredFields = ["Email", "Rating", "ActiveSince"];

    public static (CsvImportResult Result, List<DriverProfile> Profiles) Import(Stream csvStream)
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

        var profiles = new List<DriverProfile>();
        var errors = new List<string>();
        var rowNumber = 1; // the header is row 1

        while (csv.Read())
        {
            rowNumber++;
            try
            {
                profiles.Add(BuildProfile(csv, mapping));
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNumber}: {ex.Message}");
            }
        }

        if (profiles.Count == 0 && errors.Count == 0)
        {
            return (CsvImportResult.Failure("The file has a header row but no data row.", headers, mapping), []);
        }

        return (CsvImportResult.Success(headers, mapping, rowNumber - 1, profiles.Count, errors), profiles);
    }

    private static DriverProfile BuildProfile(CsvReader csv, Dictionary<string, string> mapping)
    {
        var email = csv.GetField(mapping["Email"]);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new FormatException("Email is missing.");
        }

        var rating = CsvHeaderMapper.ParseDecimal(csv.GetField(mapping["Rating"]), "rating");
        var activeSince = CsvHeaderMapper.ParseDateTime(csv.GetField(mapping["ActiveSince"]), "active since");

        return new DriverProfile
        {
            ImportedAtUtc = DateTime.UtcNow,
            Email = email,
            Rating = rating,
            ActiveSince = activeSince,
            FirstName = GetOptional(csv, mapping, "FirstName") ?? "",
            LastName = GetOptional(csv, mapping, "LastName") ?? "",
            PreferredName = GetOptional(csv, mapping, "PreferredName"),
            PhoneNumber = GetOptional(csv, mapping, "PhoneNumber") ?? "",
            DriversUnderPartner = ParseOptionalInt(GetOptional(csv, mapping, "DriversUnderPartner")),
            ProfileId = GetOptional(csv, mapping, "ProfileId"),
            IsPartner = CsvHeaderMapper.TryParseBool(GetOptional(csv, mapping, "IsPartner")),
            OptedInToEmail = CsvHeaderMapper.TryParseBool(GetOptional(csv, mapping, "OptedInToEmail")),
            OptedInToSms = CsvHeaderMapper.TryParseBool(GetOptional(csv, mapping, "OptedInToSms")),
            LanguagePreference = GetOptional(csv, mapping, "LanguagePreference"),
            PreferredLanguageScript = GetOptional(csv, mapping, "PreferredLanguageScript"),
            LifetimeCompletedTrips = ParseOptionalInt(GetOptional(csv, mapping, "LifetimeCompletedTrips")),
            OperatingCity = GetOptional(csv, mapping, "OperatingCity"),
            OperatingCountry = GetOptional(csv, mapping, "OperatingCountry"),
            FleetType = GetOptional(csv, mapping, "FleetType"),
            ReferralCode = GetOptional(csv, mapping, "ReferralCode"),
            SignupCity = GetOptional(csv, mapping, "SignupCity"),
            SignupDate = CsvHeaderMapper.TryParseDateTime(GetOptional(csv, mapping, "SignupDate")),
        };
    }

    private static int? ParseOptionalInt(string? value) =>
        int.TryParse(value, out var result) ? result : null;

    private static string? GetOptional(CsvReader csv, Dictionary<string, string> mapping, string field) =>
        mapping.TryGetValue(field, out var header) ? csv.GetField(header) : null;
}
