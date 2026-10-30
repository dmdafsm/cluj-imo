namespace ClujImo.Domain.Classification;

public sealed record WorkClassification(
    WorkCategory Category,
    bool IsContinuation,
    bool IsAmendment,
    bool IncludesDemolition,
    bool IsUrbanPlan,
    string? HeightRegime,
    int? LevelsAboveGround,
    int? Units)
{
    /// <summary>A new building authorized here; continuations and amendments of earlier permits do not count again.</summary>
    public bool IsNewBuild => Category.IsNewBuild() && !IsContinuation && !IsAmendment;
}
