using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace ClujImo.Infrastructure.Sources.CityHall;

public sealed record DecisionListItem(int Number, int Year, DateOnly? Date, string Title, string Url);

/// <summary>Parses the city hall's WordPress pages. Kept free of I/O so it can be tested with saved HTML.</summary>
public static partial class CityHallPages
{
    private static readonly CultureInfo Ro = CultureInfo.GetCultureInfo("ro-RO");

    public static IReadOnlyList<DecisionListItem> Decisions(string html)
    {
        var items = new List<DecisionListItem>();
        foreach (Match article in Article().Matches(html))
        {
            var body = article.Value;
            var link = DecisionLink().Match(body);
            if (!link.Success)
            {
                continue;
            }

            var date = DateLabel().Match(body);
            var title = Paragraph().Match(body);
            items.Add(new DecisionListItem(
                int.Parse(link.Groups["number"].ValueSpan, provider: null),
                int.Parse(link.Groups["year"].ValueSpan, provider: null),
                date.Success && DateOnly.TryParseExact(Text(date.Groups[1].Value), "d MMMM yyyy", Ro, DateTimeStyles.None, out var d) ? d : null,
                title.Success ? Text(title.Groups[1].Value) : string.Empty,
                link.Groups["url"].Value));
        }

        return items;
    }

    /// <summary>Links to the monthly intent tables ("Anunturi-intentie-1-30.04.2026.pdf").</summary>
    public static IReadOnlyList<Uri> IntentTables(string html) =>
        [.. IntentPdf().Matches(html).Select(m => new Uri(m.Groups[1].Value)).Distinct()];

    private static string Text(string html) =>
        WebUtility.HtmlDecode(Tags().Replace(html, string.Empty)).Replace(' ', ' ').Trim();

    [GeneratedRegex(@"<article\b.*?</article>", RegexOptions.Singleline)]
    private static partial Regex Article();

    [GeneratedRegex(@"href=""(?<url>[^""]+/hotararea-(?<number>\d+)-din-(?<year>\d{4})/?)""")]
    private static partial Regex DecisionLink();

    [GeneratedRegex(@"class=""date-label"">([^<]+)<")]
    private static partial Regex DateLabel();

    [GeneratedRegex(@"<p>(.*?)</p>", RegexOptions.Singleline)]
    private static partial Regex Paragraph();

    [GeneratedRegex(@"href=""(https://files\.primariaclujnapoca\.ro/[^""]*Anunturi-intentie[^""]*\.pdf)""", RegexOptions.IgnoreCase)]
    private static partial Regex IntentPdf();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();
}
