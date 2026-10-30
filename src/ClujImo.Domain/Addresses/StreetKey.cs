using System.Text.RegularExpressions;
using ClujImo.Domain.Text;

namespace ClujImo.Domain.Addresses;

/// <summary>Reduces a street name to a comparable key: no type ("Strada", "B-dul"), no titles, no punctuation.</summary>
public static partial class StreetKey
{
    private static readonly HashSet<string> TypeWords =
    [
        "STRADA", "STR", "ALEEA", "AL", "BULEVARDUL", "BULEVARD", "BDUL", "BD", "BLVD", "CALEA", "PIATA", "PTA",
        "PIATETA", "SPLAIUL", "PASAJUL", "INTRAREA", "SOSEAUA", "SOS", "TR", // "tr." is a truncated "Str." in some exports.
    ];

    private static readonly HashSet<string> TitleWords =
    [
        "GENERAL", "GRAL", "PROF", "PROFESOR", "DR", "DOCTOR", "ACAD", "ACADEMICIAN", "ING", "INGINER", "AVIATOR",
        "MARESAL", "LT", "LOCOTENENT", "COLONEL", "COL", "CPT", "CAPITAN", "PREOT", "EPISCOP", "MITROPOLIT",
        "CARDINAL", "POET", "PICTOR", "COMPOZITOR", "SF", "SFANTUL",
    ];

    public static string? From(string? streetName)
    {
        var text = TextNormalizer.ForMatching(streetName);
        if (text.Length == 0)
        {
            return null;
        }

        text = Abbreviation().Replace(text, m => m.Groups[1].Value switch
        {
            "G" => "GENERAL",
            "B" => "BULEVARDUL",
            "P" => "PIATA",
            _ => m.Value,
        });

        var tokens = NonAlphanumeric().Split(text)
            .Where(t => t.Length > 0 && !TypeWords.Contains(t) && !TitleWords.Contains(t))
            .ToArray();

        return tokens.Length == 0 ? null : string.Join(' ', tokens);
    }

    [GeneratedRegex(@"\b(G|B|P)-(RAL|DUL|TA)\b")]
    private static partial Regex Abbreviation();

    [GeneratedRegex("[^A-Z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
