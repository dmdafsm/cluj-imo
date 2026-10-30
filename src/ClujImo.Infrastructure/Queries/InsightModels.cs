using ClujImo.Domain.Classification;
using ClujImo.Domain.Geo;
using ClujImo.Domain.Planning;

namespace ClujImo.Infrastructure.Queries;

public sealed record MapCluster(double Latitude, double Longitude, int Count, int NewBuilds, int Units, WorkCategory TopCategory,
    GeoPrecision Precision, string Label, IReadOnlyList<MapItem> Items);

public sealed record MapItem(int Id, string Title, string Kind);

public sealed record PlanRow(int Id, UrbanPlanStage Stage, UrbanPlanKind Kind, string Reference, DateOnly? Date, string Title,
    string? Initiator, string? Neighborhood, WorkCategory Category, string? SourceUrl);

public sealed record PlanPoint(double Latitude, double Longitude, UrbanPlanStage Stage, UrbanPlanKind Kind, string Title, DateOnly? Date, string? Url);

public sealed record SitePoint(double Latitude, double Longitude, string? Name, string? Building);

public sealed record MonthCount(int Year, int Month, int First, int Second);

public sealed record NamedTotal(string Name, int Count, decimal Total);

public sealed record ContractRow(DateOnly? Date, string? Winner, string? CpvName, decimal? Value, string? Procedure);

public sealed record DecisionRow(int Number, int Year, DateOnly? Date, string Title, string Url);

public sealed record YearTotal(int Year, int Count, decimal Total);

public sealed record LocalitySeries(string Siruta, string Locality, IReadOnlyDictionary<int, decimal> Values)
{
    public decimal Sum(int fromYear) => Values.Where(v => v.Key >= fromYear).Sum(v => v.Value);
}

public sealed record SourceStatus(string Source, DateTimeOffset? LastRun, bool? LastSucceeded, DateTimeOffset? LastSuccess, int Rows, string? Error);
