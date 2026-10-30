namespace ClujImo.Domain.Planning;

public enum UrbanPlanKind
{
    /// <summary>Plan Urbanistic de Detaliu — one plot or a few.</summary>
    Pud = 1,

    /// <summary>Plan Urbanistic Zonal — a whole area; usually precedes large developments.</summary>
    Puz = 2,
}

public enum UrbanPlanStage
{
    /// <summary>Anunț de intenție: the documentation was filed for public consultation.</summary>
    Intent = 1,

    /// <summary>Approved by a Local Council decision (HCL).</summary>
    Approved = 2,
}
