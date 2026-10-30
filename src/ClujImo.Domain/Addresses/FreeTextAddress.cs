using System.Text.RegularExpressions;

namespace ClujImo.Domain.Addresses;

/// <summary>Finds the first street mention in free text: "..., str. Paris, nr.70-72", "B-dul Muncii - nord".</summary>
public static partial class FreeTextAddress
{
    public static ParsedAddress? Find(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = StreetMention().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var type = match.Groups["type"].Value;
        var name = match.Groups["name"].Value.Trim(' ', ',', '.', '-', '–');
        // Keep "Calea" in the display name; other type words are noise.
        var street = type.StartsWith("calea", StringComparison.OrdinalIgnoreCase) ? $"Calea {name}" : name;
        var number = match.Groups["number"].Success ? match.Groups["number"].Value : null;
        return new ParsedAddress(street, StreetKey.From(street), number, false);
    }

    [GeneratedRegex(@"(?<type>\bstr(?:ada|\.)|\bb-dul|\bbd\.|\bbdul\.?|\bbvd\.?|\bbulevardul|\bcalea|\baleea|\bpia[tț]a|\bp-[tț]a)\s*(?<name>[\p{Lu}0-9][\p{L}0-9\.\- ]{1,40}?)(?=\s*(?:,|;|\(|–|\s-\s|\bnr\b|\bzona\b|\blatura\b|$))(?:\s*,?\s*nr\.?\s*(?<number>\d{1,4}\s?[A-Za-z]?))?", RegexOptions.IgnoreCase)]
    private static partial Regex StreetMention();
}
