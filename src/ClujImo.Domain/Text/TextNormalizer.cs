using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ClujImo.Domain.Text;

/// <summary>Canonical form of source text for matching: upper case, no diacritics, single spaces.</summary>
public static partial class TextNormalizer
{
    /// <summary>Removes control characters and the literal "\n" escapes the source leaves in descriptions.</summary>
    public static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = HtmlTag().Replace(value, " ")
            .Replace("\\n", " ", StringComparison.Ordinal)
            .Replace("\\r", " ", StringComparison.Ordinal);
        var builder = new StringBuilder(text.Length);
        var previousWasSpace = false;
        foreach (var ch in text)
        {
            var isSpace = char.IsWhiteSpace(ch) || char.IsControl(ch);
            if (isSpace && previousWasSpace)
            {
                continue;
            }

            builder.Append(isSpace ? ' ' : ch);
            previousWasSpace = isSpace;
        }

        return builder.ToString().Trim();
    }

    public static string ForMatching(string? value)
    {
        var cleaned = Clean(value);
        if (cleaned.Length == 0)
        {
            return cleaned;
        }

        var decomposed = cleaned.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(ch switch
            {
                // Cedilla variants survive FormD on some inputs; fold them explicitly.
                'ş' or 'ș' => 's',
                'Ş' or 'Ș' => 'S',
                'ţ' or 'ț' => 't',
                'Ţ' or 'Ț' => 'T',
                _ => ch,
            });
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    [GeneratedRegex(@"<\s*/?\s*(br|p|div|span)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTag();
}
