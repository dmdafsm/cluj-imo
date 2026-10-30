using System.Globalization;

namespace ClujImo.Web.Presentation;

/// <summary>One line per series across years; the first two series are highlighted, the rest are context.</summary>
public sealed class YearLineChart
{
    private static readonly decimal[] NiceSteps = [1m, 2m, 2.5m, 5m, 10m];

    public const int Width = 720;
    public const int Height = 280;
    public const int Left = 48;
    public const int Right = 120;
    public const int Top = 16;
    public const int Bottom = 28;

    public required IReadOnlyList<int> Years { get; init; }

    public required IReadOnlyList<Line> Lines { get; init; }

    public required decimal Max { get; init; }

    public IEnumerable<(double Y, decimal Value)> GridLines =>
        Enumerable.Range(0, 5).Select(i => (ScaleY(Max * i / 4), Max * i / 4));

    public double X(int year) => Years.Count < 2 ? Left : Left + ((year - Years[0]) * (Width - Left - Right) / (double)(Years[^1] - Years[0]));

    public double ScaleY(decimal value) => Top + ((Height - Top - Bottom) * (1 - (double)(value / Max)));

    public static YearLineChart From(IEnumerable<(string Name, IReadOnlyDictionary<int, decimal> Values)> series)
    {
        var list = series.ToList();
        var years = list.SelectMany(s => s.Values.Keys).Distinct().Order().ToList();
        var maxValue = list.Count == 0 ? 1 : list.SelectMany(s => s.Values.Values).DefaultIfEmpty(1).Max();
        var chart = new YearLineChart { Years = years, Lines = [], Max = NiceMax(maxValue) };
        var lines = list.Select((s, i) =>
        {
            var points = s.Values.OrderBy(v => v.Key).Select(v => (X: chart.X(v.Key), Y: chart.ScaleY(v.Value), v.Key, v.Value)).ToList();
            var path = string.Join(' ', points.Select((p, j) => string.Create(CultureInfo.InvariantCulture, $"{(j == 0 ? 'M' : 'L')}{p.X:0.#},{p.Y:0.#}")));
            return new Line(s.Name, i == 0 ? "focus" : i == 1 ? "previous" : "context", path, points.Count > 0 ? points[^1].Y : 0, points.Count > 0 ? points[^1].Value : 0);
        }).ToList();
        return new YearLineChart { Years = years, Lines = SpreadLabels(lines), Max = chart.Max };
    }

    /// <summary>Pushes end labels apart so they do not overlap.</summary>
    private static List<Line> SpreadLabels(List<Line> lines)
    {
        const double gap = 12;
        const double lowest = Height - Bottom - 4;
        var ordered = lines.OrderBy(l => l.EndY).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].LabelY - ordered[i - 1].LabelY < gap)
            {
                ordered[i] = ordered[i] with { LabelY = ordered[i - 1].LabelY + gap };
            }
        }

        // Labels pushed below the plot area move back up, keeping their order.
        for (var i = ordered.Count - 1; i >= 0; i--)
        {
            var limit = i == ordered.Count - 1 ? lowest : ordered[i + 1].LabelY - gap;
            if (ordered[i].LabelY > limit)
            {
                ordered[i] = ordered[i] with { LabelY = limit };
            }
        }

        return [.. lines.Select(l => ordered.First(o => o.Name == l.Name))];
    }

    private static decimal NiceMax(decimal value)
    {
        var magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)Math.Max(1, value))));
        var step = NiceSteps.Select(m => m * magnitude / 4).First(s => s * 4 >= value);
        return step * 4;
    }

    public sealed record Line(string Name, string Role, string Path, double EndY, decimal EndValue)
    {
        public double LabelY { get; init; } = EndY;
    }
}
