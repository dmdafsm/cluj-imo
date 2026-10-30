namespace ClujImo.Domain.Investments;

/// <summary>A public procurement contract (SICAP export) signed by the city.</summary>
public sealed class PublicContract
{
    public int Id { get; set; }

    public string AuthorityCui { get; set; } = string.Empty;

    public string Authority { get; set; } = string.Empty;

    public string ContractNumber { get; set; } = string.Empty;

    public string? AwardNoticeNumber { get; set; }

    public string? Lot { get; set; }

    public DateOnly? ContractDate { get; set; }

    /// <summary>Lucrari / Servicii / Furnizare.</summary>
    public string? ContractType { get; set; }

    public string? Procedure { get; set; }

    public string? CpvCode { get; set; }

    public string? CpvName { get; set; }

    public decimal? ValueRon { get; set; }

    public string? Winner { get; set; }

    public string? WinnerCui { get; set; }

    public bool IsWorks => CpvCode?.StartsWith("45", StringComparison.Ordinal) == true
        || string.Equals(ContractType, "Lucrari", StringComparison.OrdinalIgnoreCase);
}
