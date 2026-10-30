using ClujImo.Domain.Classification;
using ClujImo.Domain.Permits;

namespace ClujImo.Infrastructure.Queries;

public sealed record Kpis(
    int Total,
    int NewBuilds,
    int NewCollective,
    int Units,
    int Demolitions,
    int? MedianProcessingDays,
    int? PreviousPeriodTotal,
    int? PreviousPeriodNewCollective,
    DateOnly? DataUntil);

public sealed record MonthlyPoint(int Year, int Month, int Count);

public sealed record CategoryCount(WorkCategory Category, int Count);

public sealed record AreaCount(string Name, int Count, int NewBuilds, int Units);

public sealed record PermitRow(
    int Id,
    PermitKind Kind,
    int Year,
    string Number,
    DateOnly? IssuedOn,
    string? Street,
    string? HouseNumber,
    string? Neighborhood,
    WorkCategory Category,
    string? HeightRegime,
    int? Units,
    string Description);

public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int PageCount => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public bool HasNext => PageNumber < PageCount;

    public bool HasPrevious => PageNumber > 1;
}

public sealed record PipelineRow(string Neighborhood, int Certificates, int Permits, int Completions);
