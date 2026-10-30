using Microsoft.EntityFrameworkCore;
using ClujImo.Domain.Classification;
using ClujImo.Domain.Permits;

namespace ClujImo.Infrastructure.Queries;

/// <summary>Filters shared by every view. All optional; bound straight from the query string.</summary>
public sealed record PermitFilter
{
    public PermitKind Kind { get; init; } = PermitKind.BuildingPermit;

    public int? Year { get; init; }

    public CategoryGroup Group { get; init; } = CategoryGroup.All;

    public string? Neighborhood { get; init; }

    public string? Search { get; init; }
}

public enum CategoryGroup
{
    All = 0,
    NewBuilds = 1,
    Collective = 2,
    SingleFamily = 3,
    NonResidential = 4,
    Interventions = 5,
    Demolitions = 6,
}

internal static class PermitFilterExtensions
{
    public static IQueryable<Permit> Apply(this IQueryable<Permit> query, PermitFilter filter, bool includeYear = true)
    {
        query = query.Where(p => p.Kind == filter.Kind);
        if (includeYear && filter.Year is { } year)
        {
            query = query.Where(p => p.Year == year);
        }

        if (!string.IsNullOrWhiteSpace(filter.Neighborhood))
        {
            query = query.Where(p => p.Neighborhood == filter.Neighborhood);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Description, term) || EF.Functions.Like(p.RawAddress, term)
                || (p.StreetDisplay != null && EF.Functions.Like(p.StreetDisplay, term)) || p.Number == filter.Search.Trim());
        }

        return filter.Group switch
        {
            CategoryGroup.NewBuilds => query.Where(p => p.IsNewBuild),
            CategoryGroup.Collective => query.Where(p => p.IsNewBuild).Where(p => p.Category == WorkCategory.NewCollectiveResidential
                || p.Category == WorkCategory.NewMixedUse || p.Category == WorkCategory.NewSemiCollectiveResidential),
            CategoryGroup.SingleFamily => query.Where(p => p.IsNewBuild && p.Category == WorkCategory.NewSingleFamily),
            CategoryGroup.NonResidential => query.Where(p => p.IsNewBuild).Where(p => p.Category == WorkCategory.NewOffice || p.Category == WorkCategory.NewCommercial
                || p.Category == WorkCategory.NewIndustrial || p.Category == WorkCategory.NewPublic || p.Category == WorkCategory.NewOtherBuilding),
            CategoryGroup.Interventions => query.Where(p => p.Category == WorkCategory.Extension || p.Category == WorkCategory.Renovation),
            CategoryGroup.Demolitions => query.Where(p => p.IncludesDemolition),
            _ => query,
        };
    }
}
