using System.Text.Json;
using ClujImo.Domain.Addresses;

namespace ClujImo.Infrastructure.Sources;

/// <summary>Parses the street nomenclator (name, former name, fiscal zone, area).</summary>
public static class NomenclatorParser
{
    public static IReadOnlyList<Street> Parse(ReadOnlySpan<byte> utf8)
    {
        var reader = new Utf8JsonReader(utf8.StartsWith("﻿"u8) ? utf8[3..] : utf8);
        using var document = JsonDocument.ParseValue(ref reader);

        var streets = new Dictionary<string, Street>(StringComparer.Ordinal);
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var name = element.TryGetProperty("denumire", out var n) ? n.GetString()?.Trim() : null;
            var key = StreetKey.From(name);
            if (name is null || key is null)
            {
                continue;
            }

            var (zone, area) = ReadFiscalZone(element);
            var formerName = element.TryGetProperty("den_veche", out var f) ? f.GetString()?.Trim() : null;
            streets.TryAdd(key, new Street
            {
                Key = key,
                Name = name,
                FormerName = string.IsNullOrEmpty(formerName) ? null : formerName,
                FiscalZone = zone,
                Area = area,
            });
        }

        return [.. streets.Values];
    }

    /// <summary>A street can span several zones by interval; the first active one is representative enough.</summary>
    private static (string? Zone, string? Area) ReadFiscalZone(JsonElement street)
    {
        if (!street.TryGetProperty("zona_fiscala", out var zones) || zones.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        foreach (var zone in zones.EnumerateObject())
        {
            var area = zone.Value.ValueKind == JsonValueKind.Object && zone.Value.TryGetProperty("amplasare", out var a)
                ? a.GetString()?.Trim()
                : null;
            return (zone.Name, string.IsNullOrEmpty(area) ? null : area);
        }

        return (null, null);
    }
}
