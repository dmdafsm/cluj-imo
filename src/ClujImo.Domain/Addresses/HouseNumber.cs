using System.Text.RegularExpressions;

namespace ClujImo.Domain.Addresses;

/// <summary>House numbers as written in permits ("69 A", "13-15", "178K-188S") and in OSM ("69A").</summary>
public static partial class HouseNumber
{
    /// <summary>Upper case, no spaces, first number of a range: "69 a" → "69A", "13-15" → "13".</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = Leading().Match(value.ToUpperInvariant().Replace(" ", string.Empty, StringComparison.Ordinal));
        return match.Success ? match.Value : null;
    }

    /// <summary>The numeric part: "69A" → 69.</summary>
    public static int? Numeric(string? value)
    {
        var normalized = Normalize(value);
        if (normalized is null)
        {
            return null;
        }

        var digits = Digits().Match(normalized);
        return digits.Success ? int.Parse(digits.ValueSpan, provider: null) : null;
    }

    [GeneratedRegex(@"^\d{1,4}[A-Z]?")]
    private static partial Regex Leading();

    [GeneratedRegex(@"^\d+")]
    private static partial Regex Digits();
}
