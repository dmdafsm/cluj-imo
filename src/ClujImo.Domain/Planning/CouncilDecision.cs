namespace ClujImo.Domain.Planning;

/// <summary>A Local Council decision (HCL) relevant to construction: plan approvals and public investments.</summary>
public sealed class CouncilDecision
{
    public int Id { get; set; }

    public int Number { get; set; }

    public int Year { get; set; }

    public DateOnly? Date { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public DecisionTopic Topic { get; set; }
}

public enum DecisionTopic
{
    Other = 0,

    /// <summary>Approves a PUZ or PUD.</summary>
    UrbanPlanApproval = 1,

    /// <summary>Approves documentation / indicators of a public investment objective.</summary>
    PublicInvestment = 2,
}
