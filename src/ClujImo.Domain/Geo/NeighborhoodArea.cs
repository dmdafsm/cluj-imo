using System.Text.Json;

namespace ClujImo.Domain.Geo;

/// <summary>A neighborhood (cartier) from OpenStreetMap: a polygon when mapped, otherwise only a center point.</summary>
public sealed class NeighborhoodArea
{
    private IReadOnlyList<IReadOnlyList<GeoPoint>>? _rings;

    public string Name { get; set; } = string.Empty;

    public double CenterLatitude { get; set; }

    public double CenterLongitude { get; set; }

    /// <summary>Outer rings as JSON [[[lat,lon],...],...]; null when OSM only has a point.</summary>
    public string? RingsJson { get; set; }

    public GeoPoint Center => new(CenterLatitude, CenterLongitude);

    public IReadOnlyList<IReadOnlyList<GeoPoint>> Rings => _rings ??= RingsJson is null
        ? []
        : JsonSerializer.Deserialize<double[][][]>(RingsJson)!
            .Select(ring => (IReadOnlyList<GeoPoint>)ring.Select(p => new GeoPoint(p[0], p[1])).ToList())
            .ToList();

    public bool HasPolygon => RingsJson is not null;

    public static string SerializeRings(IEnumerable<IEnumerable<GeoPoint>> rings) =>
        JsonSerializer.Serialize(rings.Select(r => r.Select(p => new[] { Math.Round(p.Latitude, 6), Math.Round(p.Longitude, 6) })));

    public bool Contains(GeoPoint point) => Rings.Any(ring => RingContains(ring, point));

    /// <summary>Ray casting; good enough for city-scale polygons.</summary>
    private static bool RingContains(IReadOnlyList<GeoPoint> ring, GeoPoint p)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var (a, b) = (ring[i], ring[j]);
            if ((a.Latitude > p.Latitude) != (b.Latitude > p.Latitude)
                && p.Longitude < ((b.Longitude - a.Longitude) * (p.Latitude - a.Latitude) / (b.Latitude - a.Latitude)) + a.Longitude)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
