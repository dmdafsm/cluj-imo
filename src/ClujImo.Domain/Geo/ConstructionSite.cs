namespace ClujImo.Domain.Geo;

/// <summary>An area mapped in OpenStreetMap as under construction (landuse/building=construction).</summary>
public sealed class ConstructionSite
{
    /// <summary>OSM type + id, e.g. "way/123".</summary>
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }

    /// <summary>What is being built, when tagged (construction=apartments, ...).</summary>
    public string? Building { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string? Neighborhood { get; set; }
}
