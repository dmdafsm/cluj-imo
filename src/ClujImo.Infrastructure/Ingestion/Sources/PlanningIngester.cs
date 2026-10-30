using System.Globalization;
using ClujImo.Domain.Planning;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Sources.CityHall;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure.Ingestion.Sources;

/// <summary>
/// Early signals from the city hall site: PUZ/PUD filed for consultation (monthly PDF tables) and Local Council
/// decisions (plan approvals, public investment objectives).
/// </summary>
public sealed partial class PlanningIngester(
    AppDbContext db,
    HttpClient http,
    IOptions<CityHallOptions> options,
    TimeProvider time,
    ILogger<PlanningIngester> logger) : ISourceIngester
{
    /// <summary>Safety stop for the paged decision list.</summary>
    private const int MaxPages = 200;

    public string Name => "planning";

    public bool Enabled => options.Value.Enabled;

    public TimeSpan RefreshInterval => options.Value.RefreshInterval;

    public async Task IngestAsync(IngestionRun run, CancellationToken cancellationToken)
    {
        await IngestIntentsAsync(run, cancellationToken).ConfigureAwait(false);
        await IngestDecisionsAsync(run, cancellationToken).ConfigureAwait(false);
    }

    private async Task IngestIntentsAsync(IngestionRun run, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var page = await http.GetStringAsync(new Uri(settings.PlanningNoticesUrl), cancellationToken).ConfigureAwait(false);
        var processed = await db.ProcessedResources.Where(r => r.Source == Name).Select(r => r.Url).ToHashSetAsync(cancellationToken).ConfigureAwait(false);
        var plans = await db.UrbanPlans.Where(p => p.Stage == UrbanPlanStage.Intent).ToDictionaryAsync(p => p.Reference, cancellationToken).ConfigureAwait(false);

        foreach (var url in CityHallPages.IntentTables(page).Where(u => UploadYear(u) >= settings.FromYear && !processed.Contains(u.ToString())))
        {
            await Task.Delay(settings.Delay, time, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<IntentTableParser.Row> rows;
            try
            {
                rows = IntentTableParser.Parse(PdfWords.Read(await http.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false)));
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or ArgumentException)
            {
                LogPdfFailed(ex, url);
                continue;
            }

            foreach (var row in rows)
            {
                run.RowsRead++;
                if (!plans.TryGetValue(row.RegistrationNumber, out var plan))
                {
                    plan = new UrbanPlan { Stage = UrbanPlanStage.Intent, Reference = row.RegistrationNumber };
                    db.UrbanPlans.Add(plan);
                    plans[row.RegistrationNumber] = plan;
                    run.RowsInserted++;
                }

                (plan.Kind, plan.Date, plan.Title, plan.Initiator, plan.SourceUrl) = (row.Kind, row.Date, row.Title, row.Initiator, url.ToString());
            }

            db.ProcessedResources.Add(new ProcessedResource { Url = url.ToString(), Source = Name, ProcessedAt = time.GetUtcNow(), Rows = rows.Count });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task IngestDecisionsAsync(IngestionRun run, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var decisions = await db.CouncilDecisions.ToDictionaryAsync(d => (d.Year, d.Number), cancellationToken).ConfigureAwait(false);
        var approvals = await db.UrbanPlans.Where(p => p.Stage == UrbanPlanStage.Approved).ToDictionaryAsync(p => p.Reference, cancellationToken).ConfigureAwait(false);
        var firstLoad = decisions.Count == 0;

        for (var pageNumber = 1; pageNumber <= MaxPages; pageNumber++)
        {
            var url = new Uri($"{settings.DecisionsUrl}?sf_paged={pageNumber.ToString(CultureInfo.InvariantCulture)}");
            var items = CityHallPages.Decisions(await http.GetStringAsync(url, cancellationToken).ConfigureAwait(false));
            var relevant = items.Where(i => i.Year >= settings.FromYear).ToList();
            var known = relevant.Count(i => decisions.ContainsKey((i.Year, i.Number)));

            foreach (var item in relevant)
            {
                run.RowsRead++;
                var (title, initiator) = DecisionClassifier.SplitBeneficiary(item.Title);
                var topic = DecisionClassifier.Classify(title);
                if (!decisions.TryGetValue((item.Year, item.Number), out var decision))
                {
                    decision = new CouncilDecision { Year = item.Year, Number = item.Number };
                    db.CouncilDecisions.Add(decision);
                    decisions[(item.Year, item.Number)] = decision;
                    run.RowsInserted++;
                }

                (decision.Date, decision.Title, decision.Url, decision.Topic) = (item.Date, title, item.Url, topic);

                if (topic == DecisionTopic.UrbanPlanApproval && DecisionClassifier.PlanKind(title) is { } kind)
                {
                    var reference = $"{item.Number}/{item.Year}";
                    if (!approvals.TryGetValue(reference, out var plan))
                    {
                        plan = new UrbanPlan { Stage = UrbanPlanStage.Approved, Reference = reference };
                        db.UrbanPlans.Add(plan);
                        approvals[reference] = plan;
                    }

                    (plan.Kind, plan.Date, plan.Title, plan.Initiator, plan.SourceUrl) = (kind, item.Date, title, initiator, item.Url);
                }
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Done: past the years we keep, an empty page, or (after the first load) a page we already have.
            if (items.Count == 0 || relevant.Count < items.Count || (!firstLoad && known == relevant.Count))
            {
                break;
            }

            await Task.Delay(settings.Delay, time, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Files live under files.primariaclujnapoca.ro/YYYY/MM/DD/.</summary>
    private static int UploadYear(Uri url) =>
        url.Segments.Length > 1 && int.TryParse(url.Segments[1].TrimEnd('/'), out var year) ? year : 0;

    [LoggerMessage(LogLevel.Warning, "Could not read intent table {Url}")]
    private partial void LogPdfFailed(Exception ex, Uri url);
}
