using System.Globalization;
using CsvHelper.Configuration;

namespace UberGenius.Api.Imports;

public static class CsvHeaderMapper
{
    public static CsvConfiguration CreateConfig() => new(CultureInfo.InvariantCulture)
    {
        MissingFieldFound = null,
        HeaderValidated = null,
        BadDataFound = null,
        DetectDelimiter = true,
    };

    // Alias-priority-ordered: iterates aliases in preference order first, so an earlier
    // (more specific) alias wins even if a later alias's column happens to appear first
    // in the file. (A header-order-first scan would silently prefer whichever matching
    // column comes first in the CSV, regardless of which alias is the better match.)
    public static Dictionary<string, string> MapFields(string[] headers, Dictionary<string, string[]> fieldAliases)
    {
        var result = new Dictionary<string, string>();
        foreach (var (field, aliases) in fieldAliases)
        {
            foreach (var alias in aliases)
            {
                var match = headers.FirstOrDefault(h => Normalize(h) == Normalize(alias));
                if (match is not null)
                {
                    result[field] = match;
                    break;
                }
            }
        }

        return result;
    }

    public static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    public static DateTime ParseDateTime(string? value, string fieldLabel) =>
        TryParseDateTime(value) ?? throw new FormatException($"Could not parse {fieldLabel} value '{value}' as a date/time.");

    public static DateTime? TryParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // RoundtripKind, not None: a Z-suffixed or offset timestamp (e.g. "2025-06-27T13:00:07.000Z")
        // must be taken as the UTC instant it already is, not converted to the machine's local
        // zone — DateTimeStyles.None does the latter and silently shifts every such value by
        // the local UTC offset. A plain, zone-less timestamp (the original export's format)
        // still comes back Kind=Unspecified with its value untouched, exactly as before.
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ||
            DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out result))
        {
            return result;
        }

        return null;
    }

    public static decimal ParseDecimal(string? value, string fieldLabel) =>
        TryParseDecimal(value) ?? throw new FormatException($"Could not parse {fieldLabel} value '{value}' as a number.");

    public static decimal? TryParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var isParenthesized = trimmed.StartsWith('(') && trimmed.EndsWith(')');
        var cleaned = new string(value.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());

        if (!decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            return null;
        }

        return isParenthesized ? -Math.Abs(result) : result;
    }

    public static double? TryParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    public static bool? TryParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "true" or "yes" or "y" or "1" => true,
            "false" or "no" or "n" or "0" => false,
            _ => null,
        };
    }
}
