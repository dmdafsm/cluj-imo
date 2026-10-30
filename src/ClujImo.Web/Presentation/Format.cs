using System.Globalization;

namespace ClujImo.Web.Presentation;

public static class Format
{
    private static readonly CultureInfo Ro = CultureInfo.GetCultureInfo("ro-RO");

    public static string Number(int value) => value.ToString("N0", Ro);

    public static string Date(DateOnly? value) => value?.ToString("d MMM yyyy", Ro) ?? "—";

    public static string Invariant(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>"+12%" / "−8%" against a previous value, or null when there is nothing to compare.</summary>
    public static string? Change(int current, int? previous)
    {
        if (previous is not > 0)
        {
            return null;
        }

        var change = (current - previous.Value) / (double)previous.Value;
        return $"{(change >= 0 ? "+" : "−")}{Math.Abs(change).ToString("P0", Ro).Replace(" ", string.Empty, StringComparison.Ordinal)}";
    }
}
