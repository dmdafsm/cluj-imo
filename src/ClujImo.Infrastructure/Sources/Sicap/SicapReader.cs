using System.Globalization;
using System.Text;
using ClujImo.Domain.Investments;
using ClujImo.Domain.Permits;
using ClujImo.Domain.Text;
using ExcelDataReader;

namespace ClujImo.Infrastructure.Sources.Sicap;

/// <summary>
/// Streams SICAP contract exports (xlsx, xls, csv or ods; column order and naming vary by year) and keeps the rows
/// of the chosen contracting authorities. Columns are located by their normalized header text.
/// </summary>
public static class SicapReader
{
    /// <summary>Older exports name some columns differently; they map onto the current names.</summary>
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["CUI"] = "CUI AUTORITATE CONTRACTANTA",
        ["OFERTANT"] = "OFERTANT CASTIGATOR",
        ["CUI OFERTANT"] = "CUI OFERTANT CASTIGATOR",
    };

    static SicapReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static IEnumerable<PublicContract> Read(Stream stream, string extension, IReadOnlySet<string> authorityCuis)
    {
        Dictionary<string, int>? columns = null;
        foreach (var row in Rows(stream, extension))
        {
            // Sheet boundary: every sheet of an .xls export repeats the header.
            if (row.Length == 0)
            {
                columns = null;
                continue;
            }

            if (columns is null)
            {
                columns = Header(row);
                if (!columns.ContainsKey("CUI AUTORITATE CONTRACTANTA"))
                {
                    columns = null; // Title rows above the header in some exports.
                }

                continue;
            }

            string? Field(string name) => columns.TryGetValue(name, out var i) && i < row.Length && !string.IsNullOrWhiteSpace(row[i]) ? row[i]!.Trim() : null;

            var cui = Digits(Field("CUI AUTORITATE CONTRACTANTA"));
            if (cui is not null && authorityCuis.Contains(cui) && Map(Field, cui) is { } contract)
            {
                yield return contract;
            }
        }
    }

    private static IEnumerable<string?[]> Rows(Stream stream, string extension)
    {
        if (extension.Equals(".ods", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var row in OdsRows.Read(stream))
            {
                yield return row;
            }

            yield break;
        }

        using var reader = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration { FallbackEncoding = Encoding.UTF8, AutodetectSeparators = [',', ';', '\t', '|', '^'] })
            : ExcelReaderFactory.CreateReader(stream);
        do
        {
            while (reader.Read())
            {
                var row = new string?[reader.FieldCount];
                for (var i = 0; i < row.Length; i++)
                {
                    row[i] = reader.GetValue(i) switch
                    {
                        DateTime date => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                        double number => number.ToString(CultureInfo.InvariantCulture),
                        null => null,
                        var value => value.ToString(),
                    };
                }

                yield return row;
            }

            yield return [];
        }
        while (reader.NextResult());
    }

    public static PublicContract? Map(Func<string, string?> field, string cui)
    {
        var number = field("NUMAR CONTRACT");
        if (string.IsNullOrWhiteSpace(number))
        {
            return null;
        }

        var cpv = field("COD CPV");
        return new PublicContract
        {
            AuthorityCui = cui,
            Authority = field("AUTORITATE CONTRACTANTA") ?? string.Empty,
            ContractNumber = number.Trim(),
            AwardNoticeNumber = field("NUMAR ANUNT ATRIBUIRE")?.Trim(),
            Lot = field("NUMAR LOT")?.Trim(),
            // xls/xlsx give "06/10/2025 00:00:00" or a date; ods gives "2025-10-06T00:00:00".
            ContractDate = SourceDate.Parse(field("DATA CONTRACT")?.Split('T', ' ')[0]),
            ContractType = field("TIP CONTRACT"),
            Procedure = field("TIP PROCEDURA"),
            CpvCode = cpv?.Split('-', 2)[0].Trim(),
            CpvName = field("DENUMIRE CPV"),
            ValueRon = ParseDecimal(field("VALOARE CONTRACT (RON)") ?? field("VALOARE CONTRACT RON") ?? field("VALOARE RON")),
            Winner = field("OFERTANT CASTIGATOR"),
            WinnerCui = field("CUI OFERTANT CASTIGATOR"),
        };
    }

    private static Dictionary<string, int> Header(string?[] row)
    {
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < row.Length; i++)
        {
            var name = TextNormalizer.ForMatching(row[i]);
            if (name.Length > 0)
            {
                columns.TryAdd(Aliases.GetValueOrDefault(name, name), i);
            }
        }

        return columns;
    }

    private static string? Digits(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value?.Replace(" ", string.Empty, StringComparison.Ordinal), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
}
