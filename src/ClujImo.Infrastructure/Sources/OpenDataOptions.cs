using System.ComponentModel.DataAnnotations;

namespace ClujImo.Infrastructure.Sources;

public sealed class OpenDataOptions
{
    public const string Section = "OpenData";

    [Required, Url]
    public string CkanApiUrl { get; set; } = "https://data.gov.ro/api/3/action/";

    [Required]
    public string BuildingPermitsDataset { get; set; } = "autorizatii_construire";

    [Required]
    public string UrbanismCertificatesDataset { get; set; } = "certificate_urbanism";

    [Required]
    public string CompletionCertificatesDataset { get; set; } = "certificat_atestare";

    [Required]
    public string StreetNomenclatorDataset { get; set; } = "nomenclatorul-stradal-al-municipiului-cluj-napoca";

    /// <summary>How often the background worker re-downloads everything.</summary>
    [Range(typeof(TimeSpan), "00:10:00", "30.00:00:00")]
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(24);

    public bool RefreshOnStartup { get; set; } = true;
}
