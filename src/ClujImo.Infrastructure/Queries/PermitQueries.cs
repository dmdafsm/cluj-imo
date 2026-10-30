using ClujImo.Domain.Addresses;
using ClujImo.Domain.Classification;
using ClujImo.Domain.Permits;
using ClujImo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClujImo.Infrastructure.Queries;

/// <summary>Read-side queries for the UI. Everything is no-tracking.</summary>
public sealed class PermitQueries(AppDbContext db)
{
    private IQueryable<Permit> Permits => db.Permits.AsNoTracking();

    public async Task<IReadOnlyList<int>> GetYearsAsync(CancellationToken cancellationToken) =>
        await Permits.Select(p => p.Year).Distinct().OrderByDescending(y => y).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<string>> GetNeighborhoodsAsync(CancellationToken cancellationToken) =>
        await Permits.Where(p => p.Neighborhood != null && p.Kind != PermitKind.CompletionCertificate)
            .GroupBy(p => p.Neighborhood!)
            .Where(g => g.Count() >= 5)
            .Select(g => g.Key)
            .OrderBy(n => n)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<Kpis> GetKpisAsync(PermitFilter filter, CancellationToken cancellationToken)
    {
        var current = Permits.Apply(filter);
        var stats = await current
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                NewBuilds = g.Count(p => p.IsNewBuild),
                NewCollective = g.Count(p => p.IsNewBuild && (p.Category == WorkCategory.NewCollectiveResidential || p.Category == WorkCategory.NewMixedUse)),
                Units = g.Sum(p => p.IsNewBuild ? p.Units ?? 0 : 0),
                Demolitions = g.Count(p => p.IncludesDemolition),
                Until = g.Max(p => p.IssuedOn),
            })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var durations = await current
            .Where(p => p.IssuedOn != null && p.RequestedOn != null)
            .Select(p => new { p.IssuedOn, p.RequestedOn })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var days = durations
            .Select(d => d.IssuedOn!.Value.DayNumber - d.RequestedOn!.Value.DayNumber)
            .Where(d => d is >= 0 and < 3650)
            .Order()
            .ToList();

        int? previousTotal = null, previousCollective = null;
        if (filter.Year is { } year && stats?.Until is { } until)
        {
            // Same period of the previous year, so a running year compares fairly.
            var cutoff = new DateOnly(year - 1, until.Month, Math.Min(until.Day, DateTime.DaysInMonth(year - 1, until.Month)));
            var previous = Permits.Apply(filter with { Year = year - 1 }).Where(p => p.IssuedOn <= cutoff);
            previousTotal = await previous.CountAsync(cancellationToken).ConfigureAwait(false);
            previousCollective = await previous
                .CountAsync(p => p.IsNewBuild && (p.Category == WorkCategory.NewCollectiveResidential || p.Category == WorkCategory.NewMixedUse), cancellationToken)
                .ConfigureAwait(false);
            if (previousTotal == 0)
            {
                previousTotal = previousCollective = null;
            }
        }

        return new Kpis(
            stats?.Total ?? 0,
            stats?.NewBuilds ?? 0,
            stats?.NewCollective ?? 0,
            stats?.Units ?? 0,
            stats?.Demolitions ?? 0,
            days.Count > 0 ? days[days.Count / 2] : null,
            previousTotal,
            previousCollective,
            stats?.Until);
    }

    /// <summary>Counts per month for every year (the year filter is ignored so years can be compared).</summary>
    public async Task<IReadOnlyList<MonthlyPoint>> GetMonthlyAsync(PermitFilter filter, CancellationToken cancellationToken)
    {
        var dates = await Permits.Apply(filter, includeYear: false)
            .Where(p => p.IssuedOn != null)
            .GroupBy(p => new { p.IssuedOn!.Value.Year, p.IssuedOn!.Value.Month })
            .Select(g => new MonthlyPoint(g.Key.Year, g.Key.Month, g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. dates.OrderBy(p => p.Year).ThenBy(p => p.Month)];
    }

    public async Task<IReadOnlyList<CategoryCount>> GetCategoriesAsync(PermitFilter filter, CancellationToken cancellationToken) =>
        await Permits.Apply(filter)
            .GroupBy(p => p.Category)
            .OrderByDescending(g => g.Count())
            .Select(g => new CategoryCount(g.Key, g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<AreaCount>> GetTopNeighborhoodsAsync(PermitFilter filter, int take, CancellationToken cancellationToken) =>
        await Permits.Apply(filter)
            .Where(p => p.Neighborhood != null)
            .GroupBy(p => p.Neighborhood!)
            .OrderByDescending(g => g.Count(p => p.IsNewBuild)).ThenByDescending(g => g.Count())
            .Take(take)
            .Select(g => new AreaCount(g.Key, g.Count(), g.Count(p => p.IsNewBuild), g.Sum(p => p.IsNewBuild ? p.Units ?? 0 : 0)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<AreaCount>> GetTopStreetsAsync(PermitFilter filter, int take, CancellationToken cancellationToken) =>
        await Permits.Apply(filter)
            .Where(p => p.StreetDisplay != null)
            .GroupBy(p => p.StreetDisplay!)
            .OrderByDescending(g => g.Count(p => p.IsNewBuild)).ThenByDescending(g => g.Count())
            .Take(take)
            .Select(g => new AreaCount(g.Key, g.Count(), g.Count(p => p.IsNewBuild), g.Sum(p => p.IsNewBuild ? p.Units ?? 0 : 0)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<MapPoint>> GetMapPointsAsync(PermitFilter filter, CancellationToken cancellationToken)
    {
        var groups = await Permits.Apply(filter)
            .Where(p => p.StreetKey != null)
            .Join(db.Streets.AsNoTracking().Where(s => s.GeocodeStatus == GeocodeStatus.Found), p => p.StreetKey, s => s.Key, (p, s) => new { p, s })
            .GroupBy(x => new { x.s.Name, x.s.Latitude, x.s.Longitude, x.s.Neighborhood, x.p.Category })
            .Select(g => new
            {
                g.Key.Name,
                g.Key.Latitude,
                g.Key.Longitude,
                g.Key.Neighborhood,
                g.Key.Category,
                Count = g.Count(),
                NewBuilds = g.Count(x => x.p.IsNewBuild),
                Units = g.Sum(x => x.p.IsNewBuild ? x.p.Units ?? 0 : 0),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. groups
            .GroupBy(g => g.Name)
            .Select(g =>
            {
                var first = g.First();
                return new MapPoint(
                    g.Key,
                    first.Latitude!.Value,
                    first.Longitude!.Value,
                    g.Sum(x => x.Count),
                    g.Sum(x => x.NewBuilds),
                    g.Sum(x => x.Units),
                    g.OrderByDescending(x => x.Count).First().Category,
                    first.Neighborhood);
            })
            .OrderByDescending(p => p.Count)];
    }

    public async Task<Page<PermitRow>> SearchAsync(PermitFilter filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = Permits.Apply(filter);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
        var items = await query
            .OrderByDescending(p => p.IssuedOn).ThenByDescending(p => p.Year).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PermitRow(p.Id, p.Kind, p.Year, p.Number, p.IssuedOn, p.StreetDisplay, p.HouseNumber, p.Neighborhood,
                p.Category, p.HeightRegime, p.Units, p.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new Page<PermitRow>(items, page, pageSize, total);
    }

    public Task<Permit?> GetAsync(int id, CancellationToken cancellationToken) =>
        Permits.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <summary>Completion certificates that cite this building permit (same number, permit issued in that year).</summary>
    public async Task<IReadOnlyList<Permit>> GetCompletionsForAsync(Permit permit, CancellationToken cancellationToken)
    {
        if (permit.Kind != PermitKind.BuildingPermit || permit.IssuedOn is not { } issued)
        {
            return [];
        }

        return await Permits
            .Where(p => p.Kind == PermitKind.CompletionCertificate && p.ReferencedPermitNumber == permit.Number && p.ReferencedPermitDate == issued)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Leading indicator: new-collective CUs vs ACs vs completions per neighborhood over the last 12 months of data.</summary>
    public async Task<IReadOnlyList<PipelineRow>> GetPipelineAsync(int take, CancellationToken cancellationToken)
    {
        var latest = await Permits.MaxAsync(p => p.IssuedOn, cancellationToken).ConfigureAwait(false);
        if (latest is null)
        {
            return [];
        }

        var since = latest.Value.AddYears(-1);
        var rows = await Permits
            .Where(p => p.Neighborhood != null && p.IsNewBuild
                && (p.Category == WorkCategory.NewCollectiveResidential || p.Category == WorkCategory.NewMixedUse)
                && (p.IssuedOn >= since || (p.Kind == PermitKind.CompletionCertificate && p.Year >= since.Year)))
            .GroupBy(p => new { p.Neighborhood, p.Kind })
            .Select(g => new { g.Key.Neighborhood, g.Key.Kind, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. rows
            .GroupBy(r => r.Neighborhood!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PipelineRow(
                g.Key,
                g.Where(r => r.Kind == PermitKind.UrbanismCertificate).Sum(r => r.Count),
                g.Where(r => r.Kind == PermitKind.BuildingPermit).Sum(r => r.Count),
                g.Where(r => r.Kind == PermitKind.CompletionCertificate).Sum(r => r.Count)))
            .OrderByDescending(r => r.Certificates)
            .Take(take)];
    }

    public async Task<IngestionRun?> GetLastRunAsync(CancellationToken cancellationToken) =>
        await db.IngestionRuns.AsNoTracking().OrderByDescending(r => r.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public async Task<(int Total, int Found, int Pending)> GetGeocodeStatusAsync(CancellationToken cancellationToken)
    {
        var used = Permits.Where(p => p.StreetKey != null).Select(p => p.StreetKey).Distinct();
        var streets = db.Streets.AsNoTracking().Where(s => used.Contains(s.Key));
        return (await streets.CountAsync(cancellationToken).ConfigureAwait(false),
            await streets.CountAsync(s => s.GeocodeStatus == GeocodeStatus.Found, cancellationToken).ConfigureAwait(false),
            await streets.CountAsync(s => s.GeocodeStatus == GeocodeStatus.Pending, cancellationToken).ConfigureAwait(false));
    }
}
