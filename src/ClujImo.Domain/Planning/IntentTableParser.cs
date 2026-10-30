using System.Globalization;
using System.Text.RegularExpressions;
using ClujImo.Domain.Parties;
using ClujImo.Domain.Permits;

namespace ClujImo.Domain.Planning;

/// <summary>
/// Reads the monthly "Anunțuri de intenție privind elaborarea PUD/PUZ" tables. Columns: Nr. crt. | Nr. înregistrare |
/// Denumire lucrare / Amplasament | Inițiator / Beneficiar. Rows start at a "449688/6.04.2026" registration word;
/// the initiator column is recognized by its x position under the "Inițiator" header.
/// </summary>
public static partial class IntentTableParser
{
    public sealed record Row(string RegistrationNumber, DateOnly? Date, UrbanPlanKind Kind, string Title, string? Initiator);

    public static IReadOnlyList<Row> Parse(IEnumerable<PdfWord> words)
    {
        var rows = new List<Row>();
        foreach (var page in words.GroupBy(w => w.Page).OrderBy(g => g.Key))
        {
            var pageWords = page.OrderByDescending(w => Math.Round(w.Bottom)).ThenBy(w => w.Left).ToList();
            var initiatorHeader = pageWords.FirstOrDefault(w => w.Text.StartsWith("Ini", StringComparison.Ordinal) && w.Text.Contains("iator", StringComparison.Ordinal));
            var initiatorColumn = initiatorHeader == default ? double.MaxValue : initiatorHeader.Left - 16;

            string? registration = null;
            var registrationLeft = double.MinValue;
            var title = new List<string>();
            var initiator = new List<string>();

            void Flush()
            {
                if (registration is not null && ToRow(registration, title, initiator) is { } row)
                {
                    rows.Add(row);
                }

                registration = null;
                title.Clear();
                initiator.Clear();
            }

            foreach (var word in pageWords)
            {
                if (Registration().IsMatch(word.Text))
                {
                    Flush();
                    registration = word.Text;
                    registrationLeft = word.Left;
                    continue;
                }

                if (word.Text.StartsWith("DOCUMENTA", StringComparison.Ordinal))
                {
                    Flush();
                    continue;
                }

                // The "Nr. crt." column sits left of the registration number and belongs to the next row.
                if (registration is null || word.Left < registrationLeft - 2)
                {
                    continue;
                }

                (word.Left >= initiatorColumn ? initiator : title).Add(word.Text);
            }

            Flush();
        }

        return rows;
    }

    private static Row? ToRow(string registration, List<string> titleWords, List<string> initiatorWords)
    {
        var text = string.Join(' ', titleWords).Trim();
        var kind = Kind().Match(text);
        if (!kind.Success)
        {
            return null;
        }

        var parts = registration.Split('/', 2);
        var title = text[(kind.Index + kind.Length)..].Trim(' ', '-', '–');
        return new Row(
            parts[0],
            SourceDate.Parse(parts[1]),
            kind.Groups[1].Value.Equals("Z", StringComparison.OrdinalIgnoreCase) ? UrbanPlanKind.Puz : UrbanPlanKind.Pud,
            title,
            PartyName.Display(string.Join(' ', initiatorWords)));
    }

    [GeneratedRegex(@"^\d{4,7}/\d{1,2}\.\d{1,2}\.\d{4}$")]
    private static partial Regex Registration();

    [GeneratedRegex(@"P\.?\s?U\.?\s?([DZ])\.?", RegexOptions.IgnoreCase)]
    private static partial Regex Kind();
}
