using ClujImo.Domain.Classification;
using ClujImo.Domain.Geo;

namespace ClujImo.Domain.Planning;

/// <summary>A PUZ/PUD at one stage: filed for consultation (intent) or approved by the Local Council.</summary>
public sealed class UrbanPlan
{
    public int Id { get; set; }

    public UrbanPlanStage Stage { get; set; }

    public UrbanPlanKind Kind { get; set; }

    /// <summary>Registration number for intents ("449688"), HCL number for approvals ("636/2026").</summary>
    public string Reference { get; set; } = string.Empty;

    public DateOnly? Date { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Organization name, or "Persoană fizică"; private persons are never stored by name.</summary>
    public string? Initiator { get; set; }

    public string? SourceUrl { get; set; }

    // Derived
    public WorkCategory Category { get; set; }

    public string? StreetKey { get; set; }

    public string? StreetDisplay { get; set; }

    public string? HouseNumber { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public GeoPrecision GeoPrecision { get; set; }

    public string? Neighborhood { get; set; }
}
