using System.Globalization;
using ClujImo.Infrastructure.Queries;

namespace ClujImo.Web.Presentation;

/// <summary>Geometry for a server-rendered SVG chart: one line per year, months on the x axis.</summary>
public sealed class LineChart
{
    public const int Width = 720;
    public const int Height = 260;
    public const int Left = 40;
    public const int Right = 16;
    public const int Top = 16;
    public const int Bottom = 28;

    private LineChart(IReadOnlyList<Series> series, int max)
    {
        Lines = series;
        Max = max;
    }

    public IReadOnlyList<Series> Lines { get; }

    public int Max { get; }

    public IEnumerable<(double Y, int Value)> GridLines =>
        Enumerable.Range(0, 5).Select(i =>
        {
            var value = (int)Math.Round(Max * i / 4.0);
            return (ScaleY(value), value);
        });

    public static double X(int month) => Left + ((month - 1) * (Width - Left - Right) / 11.0);

    public static LineChart From(IReadOnlyList<MonthlyPoint> points, int? highlightYear)
    {
        var years = points.Select(p => p.Year).Distinct().Order().ToList();
        var focus = highlightYear ?? (years.Count > 0 ? years[^1] : 0);
        var max = NiceMax(points.Count > 0 ? points.Max(p => p.Count) : 1);
        var chart = new LineChart([], max);

        var series = years.Select(year =>
        {
            var byMonth = points.Where(p => p.Year == year).ToDictionary(p => p.Month, p => p.Count);
            var coordinates = byMonth.OrderBy(kv => kv.Key)
                .Select(kv => (X: X(kv.Key), Y: chart.ScaleY(kv.Value), Month: kv.Key, Value: kv.Value))
                .ToList();
            var role = year == focus ? SeriesRole.Focus : year == focus - 1 ? SeriesRole.Previous : SeriesRole.Context;
            return new Series(year, role, coordinates);
        }).ToList();

        return new LineChart(series, max);
    }

    private double ScaleY(int value) => Top + ((Height - Top - Bottom) * (1 - (value / (double)Max)));

    private static int NiceMax(int value)
    {
        // Four grid intervals of a round step, so every grid label is a round number.
        var step = value switch { <= 8 => 2, <= 20 => 5, <= 40 => 10, <= 100 => 25, <= 200 => 50, <= 400 => 100, <= 1000 => 250, _ => 500 };
        return Math.Max(4 * step, (int)Math.Ceiling(value / (4.0 * step)) * 4 * step);
    }

    public sealed record Series(int Year, SeriesRole Role, IReadOnlyList<(double X, double Y, int Month, int Value)> Points)
    {
        public string Path => string.Join(' ', Points.Select((p, i) =>
            string.Create(CultureInfo.InvariantCulture, $"{(i == 0 ? 'M' : 'L')}{p.X:0.#},{p.Y:0.#}")));
    }

    public enum SeriesRole
    {
        Focus,
        Previous,
        Context,
    }
}
