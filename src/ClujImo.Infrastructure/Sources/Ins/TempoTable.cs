using System.Globalization;
using System.Text.RegularExpressions;

namespace ClujImo.Infrastructure.Sources.Ins;

public sealed record TempoValue(string Siruta, string Locality, int Year, string Unit, decimal Value);

/// <summary>
/// Reads TEMPO data sets. Row 1 holds "Anul 2023" per value column, row 3 the unit; data rows hold dimension labels
/// then values. The locality label is "54975 MUNICIPIUL CLUJ-NAPOCA" (SIRUTA + name).
/// </summary>
public static partial class TempoTable
{
    private static readonly CultureInfo Ro = CultureInfo.GetCultureInfo("ro-RO");

    public static IReadOnlyList<TempoValue> Read(string[][] rows, int localityColumn)
    {
        if (rows.Length < 5)
        {
            return [];
        }

        var years = rows[1];
        var units = rows[3];
        var values = new List<TempoValue>();
        foreach (var row in rows.Skip(4))
        {
            var locality = LocalityLabel().Match(row[localityColumn]);
            if (!locality.Success)
            {
                continue;
            }

            for (var column = localityColumn + 1; column < row.Length; column++)
            {
                var year = YearLabel().Match(years[column]);
                if (year.Success && decimal.TryParse(row[column], NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                {
                    values.Add(new TempoValue(
                        locality.Groups[1].Value,
                        Ro.TextInfo.ToTitleCase(locality.Groups[2].Value.ToLower(Ro)),
                        int.Parse(year.Groups[1].ValueSpan, provider: null),
                        units[column],
                        value));
                }
            }
        }

        return values;
    }

    [GeneratedRegex(@"^\s*(\d+)\s+(?:MUNICIPIUL\s+|ORAS\s+)?(.+?)\s*$")]
    private static partial Regex LocalityLabel();

    [GeneratedRegex(@"(\d{4})")]
    private static partial Regex YearLabel();
}
