using ClujImo.Domain.Classification;
using ClujImo.Domain.Geo;

namespace ClujImo.Domain.Permits;

/// <summary>One row from any of the three permit datasets. Raw fields are kept verbatim; derived ones are rebuildable.</summary>
public sealed class Permit
{
    public int Id { get; set; }

    public PermitKind Kind { get; set; }

    public int Year { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateOnly? IssuedOn { get; set; }

    public DateOnly? RequestedOn { get; set; }

    public string? RequestNumber { get; set; }

    public string RawAddress { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? ValidityMonths { get; set; }

    /// <summary>For completion certificates: the AC they certify.</summary>
    public string? ReferencedPermitNumber { get; set; }

    public DateOnly? ReferencedPermitDate { get; set; }

    // Derived — recomputed by the enrichment step.
    public string? StreetKey { get; set; }

    public string? StreetDisplay { get; set; }

    public string? HouseNumber { get; set; }

    public bool IsZone { get; set; }

    public string? Neighborhood { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public GeoPrecision GeoPrecision { get; set; }

    public WorkCategory Category { get; set; }

    public bool IsNewBuild { get; set; }

    public bool IsContinuation { get; set; }

    public bool IsAmendment { get; set; }

    public bool IncludesDemolition { get; set; }

    public bool IsUrbanPlan { get; set; }

    public string? HeightRegime { get; set; }

    public int? LevelsAboveGround { get; set; }

    public int? Units { get; set; }

    public int? ProcessingDays => IssuedOn is { } issued && RequestedOn is { } requested && issued >= requested
        ? issued.DayNumber - requested.DayNumber
        : null;
}
