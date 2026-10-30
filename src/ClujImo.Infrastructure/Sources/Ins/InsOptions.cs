using System.ComponentModel.DataAnnotations;

namespace ClujImo.Infrastructure.Sources.Ins;

public sealed class InsOptions
{
    public const string Section = "Ins";

    public bool Enabled { get; set; } = true;

    [Required, Url]
    public string TempoUrl { get; set; } = "http://statistici.insse.ro:8077/tempo-ins/";

    /// <summary>County as labelled in TEMPO; every locality of the county is imported.</summary>
    [Required]
    public string County { get; set; } = "Cluj";

    [Range(1990, 2100)]
    public int FromYear { get; set; } = 2005;

    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromDays(7);
}
