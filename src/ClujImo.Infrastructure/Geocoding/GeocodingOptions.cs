using System.ComponentModel.DataAnnotations;

namespace ClujImo.Infrastructure.Geocoding;

public sealed class GeocodingOptions
{
    public const string Section = "Geocoding";

    public bool Enabled { get; set; } = true;

    [Required, Url]
    public string NominatimUrl { get; set; } = "https://nominatim.openstreetmap.org/";

    /// <summary>Nominatim's usage policy requires an identifying User-Agent; add a contact URL or e-mail.</summary>
    [Required]
    public string UserAgent { get; set; } = "cluj-imo/1.0 (self-hosted open-data dashboard)";

    /// <summary>Nominatim allows at most one request per second.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1.1);
}
