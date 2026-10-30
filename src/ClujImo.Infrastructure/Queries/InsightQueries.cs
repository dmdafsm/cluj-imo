using ClujImo.Domain.Investments;
using ClujImo.Domain.Planning;
using ClujImo.Domain.Statistics;
using ClujImo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClujImo.Infrastructure.Queries;

/// <summary>Read-side queries for planning, investments, the metropolitan view and map layers.</summary>
public sealed class InsightQueries(AppDbContext db)
{
    // ---- Planning (PUZ/PUD) ----

    public async Task<Page<PlanRow>> GetPlansAsync(UrbanPlanStage? stage, UrbanPlanKind? kind, int? year, string? neighborhood, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.UrbanPlans.AsNoTracking();
        if (stage is { } s)
        {
            query = query.Where(p => p.Stage == s);
        }

        if (kind is { } k)
        {
            query = query.Where(p => p.Kind == k);
        }

        if (year is { } y)
        {
            var from = new DateOnly(y, 1, 1);
            var to = new DateOnly(y, 12, 31);
            query = query.Where(p => p.Date >= from && p.Date <= to);
        }

        if (!string.IsNullOrWhiteSpace(neighborhood))
        {
            query = query.Where(p => p.Neighborhood == neighborhood);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
        var items = await query
            .OrderByDescending(p => p.Date).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PlanRow(p.Id, p.Stage, p.Kind, p.Reference, p.Date, p.Title, p.Initiator, p.Neighborhood, p.Category, p.SourceUrl))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new Page<PlanRow>(items, page, pageSize, total);
    }

    /// <summary>Per month: PUZ (First) and PUD (Second) counts for one stage.</summary>
    public async Task<IReadOnlyList<MonthCount>> GetPlanMonthsAsync(UrbanPlanStage stage, CancellationToken cancellationToken)
    {
        var rows = await db.UrbanPlans.AsNoTracking()
            .Where(p => p.Stage == stage && p.Date != null)
            .Select(p => new { p.Date, p.Kind })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. rows
            .GroupBy(r => (r.Date!.Value.Year, r.Date!.Value.Month))
            .OrderBy(g => g.Key)
            .Select(g => new MonthCount(g.Key.Year, g.Key.Month, g.Count(r => r.Kind == UrbanPlanKind.Puz), g.Count(r => r.Kind == UrbanPlanKind.Pud)))];
    }

    public async Task<IReadOnlyList<NamedTotal>> GetPlanNeighborhoodsAsync(int months, CancellationToken cancellationToken)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-months);
        return await db.UrbanPlans.AsNoTracking()
            .Where(p => p.Neighborhood != null && p.Date >= since)
            .GroupBy(p => p.Neighborhood!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new NamedTotal(g.Key, g.Count(), g.Count(p => p.Kind == UrbanPlanKind.Puz)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<int>> GetPlanYearsAsync(CancellationToken cancellationToken) =>
        await db.UrbanPlans.AsNoTracking().Where(p => p.Date != null).Select(p => p.Date!.Value.Year).Distinct()
            .OrderByDescending(y => y).ToListAsync(cancellationToken).ConfigureAwait(false);

    // ---- Investments ----

    public async Task<IReadOnlyList<YearTotal>> GetContractYearsAsync(bool worksOnly, CancellationToken cancellationToken)
    {
        var contracts = await WorksContracts(worksOnly).Where(c => c.ContractDate != null)
            .Select(c => new { c.ContractDate, c.ValueRon })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. contracts.GroupBy(c => c.ContractDate!.Value.Year).OrderBy(g => g.Key)
            .Select(g => new YearTotal(g.Key, g.Count(), g.Sum(c => c.ValueRon ?? 0)))];
    }

    public async Task<IReadOnlyList<NamedTotal>> GetTopContractorsAsync(int? year, CancellationToken cancellationToken)
    {
        var contracts = await ForYear(WorksContracts(true), year)
            .Select(c => new { c.Winner, c.ValueRon })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. contracts.Where(c => c.Winner != null)
            .GroupBy(c => c.Winner!.Trim().ToUpperInvariant())
            .Select(g => new NamedTotal(g.Key, g.Count(), g.Sum(c => c.ValueRon ?? 0)))
            .OrderByDescending(t => t.Total).Take(10)];
    }

    public async Task<IReadOnlyList<NamedTotal>> GetWorkTypesAsync(int? year, CancellationToken cancellationToken)
    {
        var contracts = await ForYear(WorksContracts(true), year)
            .Select(c => new { c.CpvName, c.ValueRon })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. contracts.Where(c => c.CpvName != null)
            .GroupBy(c => c.CpvName!.Replace(" (Rev.2)", string.Empty, StringComparison.Ordinal))
            .Select(g => new NamedTotal(g.Key, g.Count(), g.Sum(c => c.ValueRon ?? 0)))
            .OrderByDescending(t => t.Total).Take(10)];
    }

    public async Task<IReadOnlyList<ContractRow>> GetLargestContractsAsync(int? year, int take, CancellationToken cancellationToken)
    {
        var contracts = await ForYear(WorksContracts(true), year)
            .Select(c => new ContractRow(c.ContractDate, c.Winner, c.CpvName, c.ValueRon, c.Procedure))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. contracts.OrderByDescending(c => c.Value ?? 0).Take(take)];
    }

    /// <summary>Works payments per month: amount in First (thousands RON), count in Second.</summary>
    public async Task<IReadOnlyList<MonthCount>> GetWorksPaymentMonthsAsync(CancellationToken cancellationToken)
    {
        var payments = await db.PublicPayments.AsNoTracking().Where(p => p.IsWorks)
            .Select(p => new { p.Date, p.Amount })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. payments.GroupBy(p => (p.Date.Year, p.Date.Month)).OrderBy(g => g.Key)
            .Select(g => new MonthCount(g.Key.Year, g.Key.Month, (int)(g.Sum(p => p.Amount) / 1000), g.Count()))];
    }

    public async Task<IReadOnlyList<NamedTotal>> GetTopWorksBeneficiariesAsync(int? year, CancellationToken cancellationToken)
    {
        var query = db.PublicPayments.AsNoTracking().Where(p => p.IsWorks && p.Beneficiary != null);
        if (year is { } y)
        {
            var from = new DateOnly(y, 1, 1);
            var to = new DateOnly(y, 12, 31);
            query = query.Where(p => p.Date >= from && p.Date <= to);
        }

        var payments = await query.Select(p => new { p.Beneficiary, p.Amount }).ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. payments.GroupBy(p => p.Beneficiary!)
            .Select(g => new NamedTotal(g.Key, g.Count(), g.Sum(p => p.Amount)))
            .OrderByDescending(t => t.Total).Take(10)];
    }

    public async Task<IReadOnlyList<DecisionRow>> GetInvestmentDecisionsAsync(int take, CancellationToken cancellationToken) =>
        await db.CouncilDecisions.AsNoTracking()
            .Where(d => d.Topic == DecisionTopic.PublicInvestment)
            .OrderByDescending(d => d.Year).ThenByDescending(d => d.Number)
            .Take(take)
            .Select(d => new DecisionRow(d.Number, d.Year, d.Date, d.Title, d.Url))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<int>> GetInvestmentYearsAsync(CancellationToken cancellationToken)
    {
        var contractYears = await db.PublicContracts.AsNoTracking().Where(c => c.ContractDate != null)
            .Select(c => c.ContractDate!.Value.Year).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false);
        var paymentYears = await db.PublicPayments.AsNoTracking()
            .Select(p => p.Date.Year).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. contractYears.Union(paymentYears).OrderDescending()];
    }

    // ---- Metropolitan area (INS) ----

    public async Task<IReadOnlyList<LocalitySeries>> GetMetroSeriesAsync(MetroIndicator indicator, CancellationToken cancellationToken)
    {
        var stats = await db.MetroStats.AsNoTracking().Where(s => s.Indicator == indicator).ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. stats.GroupBy(s => (s.Siruta, s.Locality))
            .Select(g => new LocalitySeries(g.Key.Siruta, g.Key.Locality, g.ToDictionary(s => s.Year, s => s.Value)))];
    }

    // ---- Map layers ----

    public async Task<IReadOnlyList<PlanPoint>> GetPlanPointsAsync(int months, CancellationToken cancellationToken)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-months);
        return await db.UrbanPlans.AsNoTracking()
            .Where(p => p.Latitude != null && p.Date >= since)
            .Select(p => new PlanPoint(p.Latitude!.Value, p.Longitude!.Value, p.Stage, p.Kind, p.Title, p.Date, p.SourceUrl))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SitePoint>> GetConstructionSitesAsync(CancellationToken cancellationToken) =>
        await db.ConstructionSites.AsNoTracking()
            .Select(s => new SitePoint(s.Latitude, s.Longitude, s.Name, s.Building))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    // ---- Status ----

    public async Task<IReadOnlyList<SourceStatus>> GetSourceStatusAsync(IEnumerable<string> sources, CancellationToken cancellationToken)
    {
        var runs = await db.IngestionRuns.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. sources.Select(source =>
        {
            var mine = runs.Where(r => r.Source == source).OrderByDescending(r => r.Id).ToList();
            var last = mine.FirstOrDefault();
            var success = mine.FirstOrDefault(r => r.Succeeded);
            return new SourceStatus(source, last?.StartedAt, last?.Succeeded, success?.StartedAt, success?.RowsRead ?? 0, last is { Succeeded: false } ? last.Error : null);
        })];
    }

    private IQueryable<PublicContract> WorksContracts(bool worksOnly)
    {
        var query = db.PublicContracts.AsNoTracking();
        return worksOnly
            ? query.Where(c => (c.CpvCode != null && c.CpvCode.StartsWith("45")) || c.ContractType == "Lucrari")
            : query;
    }

    private static IQueryable<PublicContract> ForYear(IQueryable<PublicContract> query, int? year)
    {
        if (year is not { } y)
        {
            return query;
        }

        var from = new DateOnly(y, 1, 1);
        var to = new DateOnly(y, 12, 31);
        return query.Where(c => c.ContractDate >= from && c.ContractDate <= to);
    }
}
