using System.Text;
using System.Text.Json;

namespace ClujImo.Infrastructure.Sources;

/// <summary>
/// Reads the city's JSON exports, which are arrays of flat objects but may contain a BOM and
/// raw control characters inside strings (invalid JSON). Every value is returned as text.
/// </summary>
public static class LenientJson
{
    public static IReadOnlyList<IReadOnlyDictionary<string, string?>> ReadRows(ReadOnlySpan<byte> utf8)
    {
        var json = Encoding.UTF8.GetString(utf8).TrimStart('﻿');
        var sanitized = Sanitize(json);

        using var document = JsonDocument.Parse(sanitized);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException("Expected a JSON array of rows.");
        }

        var rows = new List<IReadOnlyDictionary<string, string?>>(document.RootElement.GetArrayLength());
        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                row[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    _ => property.Value.GetRawText(),
                };
            }

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>Raw control characters are whitespace outside strings and illegal inside them: a space is safe in both.</summary>
    private static string Sanitize(string json)
    {
        if (!json.Any(char.IsControl))
        {
            return json;
        }

        var builder = new StringBuilder(json.Length);
        foreach (var ch in json)
        {
            builder.Append(char.IsControl(ch) ? ' ' : ch);
        }

        return builder.ToString();
    }
}
