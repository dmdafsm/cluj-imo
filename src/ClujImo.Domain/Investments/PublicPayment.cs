namespace ClujImo.Domain.Investments;

/// <summary>A payment made by the city hall (open data "Situația plăților").</summary>
public sealed class PublicPayment
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Organization name, or "Persoană fizică".</summary>
    public string? Beneficiary { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Hash of the raw row: the source has no id, and identical payments do occur on the same day.</summary>
    public string RowKey { get; set; } = string.Empty;

    public bool IsWorks { get; set; }
}
