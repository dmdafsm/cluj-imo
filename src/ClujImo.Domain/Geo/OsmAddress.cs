namespace ClujImo.Domain.Geo;

/// <summary>An address point (addr:street + addr:housenumber) from OpenStreetMap.</summary>
public sealed class OsmAddress
{
    public int Id { get; set; }

    public string StreetKey { get; set; } = string.Empty;

    public string HouseNumber { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
