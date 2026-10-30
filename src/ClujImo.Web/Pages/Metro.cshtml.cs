using ClujImo.Domain.Statistics;
using ClujImo.Infrastructure.Queries;
using ClujImo.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClujImo.Web.Pages;

public sealed class MetroModel(InsightQueries queries) : PageModel
{
    /// <summary>SIRUTA codes always shown first: Cluj-Napoca, then Florești.</summary>
    private static readonly string[] Focus = ["54975", "57706"];

    [BindProperty(SupportsGet = true, Name = "indicator")]
    public MetroIndicator Indicator { get; set; } = MetroIndicator.DwellingsCompleted;

    public YearLineChart? Chart { get; private set; }

    public IReadOnlyList<Row> Rows { get; private set; } = [];

    public int? LatestYear { get; private set; }

    public decimal? ClujShare { get; private set; }

    public decimal? ClujShareFiveYearsAgo { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var series = await queries.GetMetroSeriesAsync(Indicator, cancellationToken);
        if (series.Count > 0)
        {
            var latest = series.SelectMany(s => s.Values.Keys).Max();
            LatestYear = latest;
            var window = latest - 4;
            var total = series.Sum(s => s.Sum(window));
            Rows = [.. series
                .Select(s => new Row(
                    s.Locality,
                    s.Values.GetValueOrDefault(latest),
                    s.Sum(window),
                    total > 0 ? s.Sum(window) / total : 0,
                    s.Values.GetValueOrDefault(latest - 5)))
                .OrderByDescending(r => r.LastFiveYears)
                .Take(15)];

            var chartSeries = series
                .OrderBy(s => Array.IndexOf(Focus, s.Siruta) is var i && i >= 0 ? i : int.MaxValue)
                .ThenByDescending(s => s.Sum(window))
                .Take(6)
                .Select(s => (s.Locality, s.Values));
            Chart = YearLineChart.From(chartSeries);

            decimal? Share(int year)
            {
                var sum = series.Sum(s => s.Values.GetValueOrDefault(year));
                var cluj = series.FirstOrDefault(s => s.Siruta == Focus[0])?.Values.GetValueOrDefault(year);
                return sum > 0 && cluj is not null ? cluj / sum : null;
            }

            ClujShare = Share(latest);
            ClujShareFiveYearsAgo = Share(latest - 5);
        }

        Response.Headers.Vary = "HX-Request";
        return Request.Headers.ContainsKey("HX-Request") && !Request.Headers.ContainsKey("HX-History-Restore-Request")
            ? Partial("_MetroResults", this)
            : Page();
    }

    public sealed record Row(string Locality, decimal Latest, decimal LastFiveYears, decimal Share, decimal FiveYearsBefore);
}
