using System.ComponentModel.DataAnnotations;

namespace ClujImo.Infrastructure.Sources.Osm;

public sealed class OsmOptions
{
    public const string Section = "Osm";

    public bool Enabled { get; set; } = true;

    /// <summary>Tried in order; the public instances are often busy.</summary>
    [MinLength(1)]
    public string[] OverpassUrls { get; set; } = ["https://overpass-api.de/api/interpreter", "https://overpass.kumi.systems/api/interpreter"];

    /// <summary>OSM name of the administrative area to query.</summary>
    [Required]
    public string AreaName { get; set; } = "Cluj-Napoca";

    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromDays(7);
}
