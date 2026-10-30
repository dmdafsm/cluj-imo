using ClujImo.Infrastructure.Queries;

namespace ClujImo.Web.Presentation;

/// <summary>Server-rendered stacked columns per month (two series at most).</summary>
public sealed class ColumnChart
{
    private static readonly double[] NiceSteps = [1, 2, 2.5, 5, 10];

    public const int Width = 720;
    public const int Height = 220;
    public const int Left = 56;
    public const int Right = 8;
    public const int Top = 12;
    public const int Bottom = 28;

    private ColumnChart(IReadOnlyList<Column> columns, int max, string firstName, string? secondName, string unit, decimal multiplier = 1)
    {
        Multiplier = multiplier;
        Columns = columns;
        Max = max;
        FirstName = firstName;
        SecondName = secondName;
        Unit = unit;
    }

    public IReadOnlyList<Column> Columns { get; }

    public int Max { get; }

    public string FirstName { get; }

    public string? SecondName { get; }

    public string Unit { get; }

    /// <summary>Values are stored scaled down (e.g. thousands of lei); labels multiply back.</summary>
    public decimal Multiplier { get; }

    public string Label(int value) => Format.Compact(value * Multiplier) + Unit;

    public double BarWidth => Columns.Count == 0 ? 0 : Math.Max(2, ((Width - Left - Right) / (double)Columns.Count) - 3);

    public IEnumerable<(double Y, int Value)> GridLines =>
        Enumerable.Range(0, 5).Select(i => (Scale(Max * i / 4), Max * i / 4));

    /// <param name="months">How many trailing months to show.</param>
    public static ColumnChart From(IReadOnlyList<MonthCount> points, int months, string firstName, string? secondName, string unit = "", decimal multiplier = 1)
    {
        var recent = points.OrderBy(p => (p.Year, p.Month)).TakeLast(months).ToList();
        var max = NiceMax(recent.Count == 0 ? 1 : recent.Max(p => p.First + (secondName is null ? 0 : p.Second)));
        var chart = new ColumnChart([], max, firstName, secondName, unit, multiplier);
        var step = (Width - Left - Right) / (double)Math.Max(1, recent.Count);
        var columns = recent.Select((p, i) =>
        {
            var x = Left + (i * step) + 1.5;
            var firstTop = chart.Scale(p.First);
            var secondValue = secondName is null ? 0 : p.Second;
            var secondTop = chart.Scale(p.First + secondValue);
            return new Column(p.Year, p.Month, x, firstTop, Height - Bottom - firstTop, secondTop, firstTop - secondTop, p.First, secondValue);
        }).ToList();
        return new ColumnChart(columns, max, firstName, secondName, unit, multiplier);
    }

    private double Scale(int value) => Top + ((Height - Top - Bottom) * (1 - (value / (double)Max)));

    private static int NiceMax(int value)
    {
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(1, value))));
        var step = NiceSteps.Select(m => m * magnitude / 4).First(s => s * 4 >= value);
        return Math.Max(4, (int)Math.Ceiling(step * 4));
    }

    public sealed record Column(int Year, int Month, double X, double FirstY, double FirstHeight, double SecondY, double SecondHeight, int First, int Second)
    {
        public bool LabelMonth => Month == 1;
    }
}
