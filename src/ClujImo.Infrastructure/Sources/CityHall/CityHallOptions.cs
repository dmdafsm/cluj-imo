using System.ComponentModel.DataAnnotations;

namespace ClujImo.Infrastructure.Sources.CityHall;

public sealed class CityHallOptions
{
    public const string Section = "CityHall";

    public bool Enabled { get; set; } = true;

    /// <summary>Page listing the monthly "Anunțuri de intenție" PDFs for PUZ/PUD.</summary>
    [Required, Url]
    public string PlanningNoticesUrl { get; set; } = "https://primariaclujnapoca.ro/strategii-urbane/informarea-si-consultarea-publicului-pud-si-puz/";

    /// <summary>Paged list of Local Council decisions (?sf_paged=N).</summary>
    [Required, Url]
    public string DecisionsUrl { get; set; } = "https://primariaclujnapoca.ro/consiliu-local/hotarari-de-consiliu/";

    [Range(2015, 2100)]
    public int FromYear { get; set; } = 2023;

    /// <summary>Pause between page requests to the city hall site.</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromDays(1);
}
