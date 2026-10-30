using System.ComponentModel.DataAnnotations;

namespace ClujImo.Infrastructure.Sources.Sicap;

public sealed class InvestmentOptions
{
    public const string Section = "Investments";

    public bool Enabled { get; set; } = true;

    /// <summary>CKAN dataset with the city hall's payments (yearly JSON resources).</summary>
    [Required]
    public string PaymentsDataset { get; set; } = "situatie_plati";

    /// <summary>CKAN dataset id per year for SICAP exports; {0} is the year.</summary>
    [Required]
    public string SicapDatasetPattern { get; set; } = "achizitii-publice-{0}";

    /// <summary>Fiscal codes (CUI) of the contracting authorities to keep. 4305857 = Municipiul Cluj-Napoca.</summary>
    [MinLength(1)]
    public string[] AuthorityCuis { get; set; } = ["4305857"];

    /// <summary>SICAP files are large (up to ~300 MB per quarter); older years are opt-in.</summary>
    [Range(2018, 2100)]
    public int SicapFromYear { get; set; } = 2024;

    public TimeSpan PaymentsRefreshInterval { get; set; } = TimeSpan.FromDays(1);

    public TimeSpan SicapRefreshInterval { get; set; } = TimeSpan.FromDays(7);
}
