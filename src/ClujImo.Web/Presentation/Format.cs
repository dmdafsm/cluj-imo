using System.Globalization;

namespace ClujImo.Web.Presentation;

public static class Format
{
    private static readonly CultureInfo Ro = CultureInfo.GetCultureInfo("ro-RO");

    public static string Number(int value) => value.ToString("N0", Ro);

    public static string Number(decimal value) => value.ToString("N0", Ro);

    /// <summary>12 400 → "12,4 mii", 3 200 000 → "3,2 mil."</summary>
    public static string Compact(decimal value) => Math.Abs(value) switch
    {
        >= 1_000_000_000 => $"{(value / 1_000_000_000).ToString("0.#", Ro)} mld.",
        >= 1_000_000 => $"{(value / 1_000_000).ToString("0.#", Ro)} mil.",
        >= 10_000 => $"{(value / 1_000).ToString("0.#", Ro)} mii",
        _ => value.ToString("N0", Ro),
    };

    public static string Lei(decimal value) => $"{Compact(value)} lei";

    private static readonly TimeZoneInfo Bucharest = FindBucharest();

    /// <summary>Times are shown in Romanian time whatever the server's time zone (containers run in UTC).</summary>
    public static string DateTime(DateTimeOffset? value) =>
        value is { } v ? TimeZoneInfo.ConvertTime(v, Bucharest).ToString("d MMM yyyy, HH:mm", Ro) : "—";

    private static TimeZoneInfo FindBucharest()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }

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
