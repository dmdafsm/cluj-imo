using System.Text.Json;
using ClujImo.Domain.Addresses;
using ClujImo.Domain.Geo;

namespace ClujImo.Infrastructure.Sources.Osm;

/// <summary>Turns Overpass JSON ("out tags center" / "out geom") into domain records.</summary>
public static class OsmParser
{
    public static IReadOnlyList<OsmAddress> Addresses(JsonDocument document)
    {
        var addresses = new Dictionary<(string Street, string Number), OsmAddress>();
        foreach (var element in Elements(document))
        {
            var tags = Tags(element);
            if (!tags.TryGetValue("addr:street", out var street) || !tags.TryGetValue("addr:housenumber", out var numbers)
                || StreetKey.From(street) is not { } key || Point(element) is not { } point)
            {
                continue;
            }

            // "10;12" or "10-12" on one building: index each number.
            foreach (var number in numbers.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                addresses.TryAdd((key, number.ToUpperInvariant()), new OsmAddress
                {
                    StreetKey = key,
                    HouseNumber = number,
                    Latitude = point.Latitude,
                    Longitude = point.Longitude,
                });
            }
        }

        return [.. addresses.Values];
    }

    public static IReadOnlyList<NeighborhoodArea> Neighborhoods(JsonDocument document)
    {
        var areas = new Dictionary<string, NeighborhoodArea>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in Elements(document))
        {
            var tags = Tags(element);
            if (!tags.TryGetValue("name", out var name))
            {
                continue;
            }

            var rings = Rings(element);
            var center = Point(element) ?? (rings.Count > 0 ? GeoPoint.Centroid(rings[0]) : (GeoPoint?)null);
            if (center is not { } c)
            {
                continue;
            }

            var area = new NeighborhoodArea
            {
                Name = name,
                CenterLatitude = c.Latitude,
                CenterLongitude = c.Longitude,
                RingsJson = rings.Count > 0 ? NeighborhoodArea.SerializeRings(rings) : null,
            };

            // A polygon wins over a bare point with the same name.
            if (!areas.TryGetValue(name, out var existing) || (!existing.HasPolygon && area.HasPolygon))
            {
                areas[name] = area;
            }
        }

        return [.. areas.Values];
    }

    public static IReadOnlyList<ConstructionSite> ConstructionSites(JsonDocument document) =>
        [.. Elements(document)
            .Select(element => (Element: element, Tags: Tags(element), Point: Point(element)))
            .Where(x => x.Point is not null)
            .Select(x => new ConstructionSite
            {
                Id = $"{x.Element.GetProperty("type").GetString()}/{Id(x.Element)}",
                Name = x.Tags.GetValueOrDefault("name"),
                Building = x.Tags.GetValueOrDefault("construction") ?? x.Tags.GetValueOrDefault("construction:building"),
                Latitude = x.Point!.Value.Latitude,
                Longitude = x.Point!.Value.Longitude,
            })
            .DistinctBy(s => s.Id)];

    private static JsonElement.ArrayEnumerator Elements(JsonDocument document) =>
        document.RootElement.GetProperty("elements").EnumerateArray();

    private static long Id(JsonElement element) => element.GetProperty("id").GetInt64();

    private static Dictionary<string, string> Tags(JsonElement element) =>
        element.TryGetProperty("tags", out var tags)
            ? tags.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty, StringComparer.Ordinal)
            : [];

    private static GeoPoint? Point(JsonElement element)
    {
        if (element.TryGetProperty("lat", out var lat) && element.TryGetProperty("lon", out var lon))
        {
            return new GeoPoint(lat.GetDouble(), lon.GetDouble());
        }

        return element.TryGetProperty("center", out var center)
            ? new GeoPoint(center.GetProperty("lat").GetDouble(), center.GetProperty("lon").GetDouble())
            : null;
    }

    /// <summary>Closed outer rings of a way or multipolygon relation ("out geom").</summary>
    private static List<List<GeoPoint>> Rings(JsonElement element)
    {
        var rings = new List<List<GeoPoint>>();
        if (element.TryGetProperty("geometry", out var geometry))
        {
            AddIfClosed(rings, geometry);
        }

        if (element.TryGetProperty("members", out var members))
        {
            foreach (var member in members.EnumerateArray())
            {
                if (member.TryGetProperty("role", out var role) && role.GetString() == "outer" && member.TryGetProperty("geometry", out var g))
                {
                    AddIfClosed(rings, g);
                }
            }
        }

        return rings;
    }

    private static void AddIfClosed(List<List<GeoPoint>> rings, JsonElement geometry)
    {
        var ring = geometry.EnumerateArray()
            .Where(p => p.ValueKind == JsonValueKind.Object)
            .Select(p => new GeoPoint(p.GetProperty("lat").GetDouble(), p.GetProperty("lon").GetDouble()))
            .ToList();
        if (ring.Count >= 4 && ring[0] == ring[^1])
        {
            rings.Add(ring);
        }
    }
}
