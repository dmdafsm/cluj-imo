namespace ClujImo.Domain.Geo;

/// <summary>Assigns a point to a neighborhood: the polygon that contains it, else the nearest mapped center.</summary>
public sealed class NeighborhoodLocator(IEnumerable<NeighborhoodArea> areas)
{
    /// <summary>Beyond this distance from any center a point is left without a neighborhood.</summary>
    private const double MaxCenterDistanceMeters = 2_000;

    private readonly List<NeighborhoodArea> _all = areas.ToList();
    private List<NeighborhoodArea> Polygons => field ??= _all.Where(a => a.HasPolygon).ToList();

    public int Count => _all.Count;

    public string? Locate(GeoPoint? point)
    {
        if (point is not { } p)
        {
            return null;
        }

        var containing = Polygons.FirstOrDefault(a => a.Contains(p));
        if (containing is not null)
        {
            return containing.Name;
        }

        var nearest = _all
            .Select(a => (Area: a, Distance: a.Center.DistanceMetersTo(p)))
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        return nearest.Area is not null && nearest.Distance <= MaxCenterDistanceMeters ? nearest.Area.Name : null;
    }
}
