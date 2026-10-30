using ClujImo.Domain.Addresses;
using ClujImo.Domain.Permits;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure.Ingestion;

/// <summary>Downloads all datasets from data.gov.ro, upserts raw rows, then rebuilds derived columns.</summary>
public sealed partial class IngestionService(
    AppDbContext db,
    CkanClient ckan,
    IOptions<OpenDataOptions> options,
    TimeProvider time,
    ILogger<IngestionService> logger)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static bool IsRunning => Gate.CurrentCount == 0;

    public async Task<IngestionRun?> RunAsync(CancellationToken cancellationToken)
    {
        if (!await Gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            LogAlreadyRunning();
            return null;
        }

        var run = new IngestionRun { StartedAt = time.GetUtcNow() };
        db.IngestionRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await IngestStreetsAsync(cancellationToken).ConfigureAwait(false);

            var settings = options.Value;
            await IngestPermitsAsync(PermitKind.BuildingPermit, settings.BuildingPermitsDataset, run, cancellationToken).ConfigureAwait(false);
            await IngestPermitsAsync(PermitKind.UrbanismCertificate, settings.UrbanismCertificatesDataset, run, cancellationToken).ConfigureAwait(false);
            await IngestPermitsAsync(PermitKind.CompletionCertificate, settings.CompletionCertificatesDataset, run, cancellationToken).ConfigureAwait(false);

            await EnrichAllAsync(db, cancellationToken).ConfigureAwait(false);
            run.Succeeded = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogIngestionFailed(ex);
            run.Error = ex.Message;
        }
        finally
        {
            run.FinishedAt = time.GetUtcNow();
            await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            Gate.Release();
        }

        LogIngestionFinished(run.RowsRead, run.RowsInserted, run.RowsUpdated, run.Succeeded);
        return run;
    }

    /// <summary>Rebuilds derived columns for every permit, e.g. after a classifier change or new geocodes.</summary>
    public static async Task EnrichAllAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var directory = new StreetDirectory(await db.Streets.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false));
        var permits = await db.Permits.ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var permit in permits)
        {
            PermitEnricher.Enrich(permit, directory);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task IngestStreetsAsync(CancellationToken cancellationToken)
    {
        var resources = await ckan.GetResourcesAsync(options.Value.StreetNomenclatorDataset, cancellationToken).ConfigureAwait(false);
        var resource = (resources.Count > 0 ? resources[0] : null) ?? throw new InvalidOperationException("Street nomenclator has no resources.");
        var streets = NomenclatorParser.Parse(await ckan.DownloadAsync(resource.Url, cancellationToken).ConfigureAwait(false));

        var existing = await db.Streets.ToDictionaryAsync(s => s.Key, cancellationToken).ConfigureAwait(false);
        foreach (var street in streets)
        {
            if (existing.TryGetValue(street.Key, out var current))
            {
                // Keep the cached geocode; refresh only nomenclator fields.
                current.Name = street.Name;
                current.FormerName = street.FormerName;
                current.FiscalZone = street.FiscalZone;
                current.Area = street.Area;
            }
            else
            {
                db.Streets.Add(street);
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogStreets(streets.Count);
    }

    private async Task IngestPermitsAsync(PermitKind kind, string datasetId, IngestionRun run, CancellationToken cancellationToken)
    {
        var resources = await ckan.GetResourcesAsync(datasetId, cancellationToken).ConfigureAwait(false);
        foreach (var resource in resources.Where(r => r.Year is not null).OrderBy(r => r.Year))
        {
            IReadOnlyList<IReadOnlyDictionary<string, string?>> rows;
            try
            {
                rows = LenientJson.ReadRows(await ckan.DownloadAsync(resource.Url, cancellationToken).ConfigureAwait(false));
            }
            catch (Exception ex) when (ex is HttpRequestException or FormatException or System.Text.Json.JsonException)
            {
                // One broken yearly file must not block the others.
                LogResourceFailed(ex, resource.Name);
                continue;
            }

            var year = resource.Year!.Value;
            var existing = await db.Permits
                .Where(p => p.Kind == kind && p.Year == year)
                .ToDictionaryAsync(p => p.Number, cancellationToken)
                .ConfigureAwait(false);

            foreach (var row in rows)
            {
                var incoming = RowMapper.Map(kind, row, year);
                if (incoming is null)
                {
                    continue;
                }

                run.RowsRead++;
                if (existing.TryGetValue(incoming.Number, out var current))
                {
                    if (CopyRaw(incoming, current))
                    {
                        run.RowsUpdated++;
                    }
                }
                else if (incoming.Year == year)
                {
                    db.Permits.Add(incoming);
                    existing[incoming.Number] = incoming;
                    run.RowsInserted++;
                }
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LogResource(resource.Name, rows.Count);
        }
    }

    private static bool CopyRaw(Permit from, Permit to)
    {
        var changed = to.IssuedOn != from.IssuedOn || to.RequestedOn != from.RequestedOn || to.RawAddress != from.RawAddress
            || to.Description != from.Description || to.ValidityMonths != from.ValidityMonths || to.RequestNumber != from.RequestNumber
            || to.ReferencedPermitNumber != from.ReferencedPermitNumber || to.ReferencedPermitDate != from.ReferencedPermitDate;

        to.IssuedOn = from.IssuedOn;
        to.RequestedOn = from.RequestedOn;
        to.RequestNumber = from.RequestNumber;
        to.RawAddress = from.RawAddress;
        to.Description = from.Description;
        to.ValidityMonths = from.ValidityMonths;
        to.ReferencedPermitNumber = from.ReferencedPermitNumber;
        to.ReferencedPermitDate = from.ReferencedPermitDate;
        return changed;
    }

    [LoggerMessage(LogLevel.Information, "Ingestion already running, skipping")]
    private partial void LogAlreadyRunning();

    [LoggerMessage(LogLevel.Error, "Ingestion failed")]
    private partial void LogIngestionFailed(Exception ex);

    [LoggerMessage(LogLevel.Information, "Ingestion finished: read {Read}, inserted {Inserted}, updated {Updated}, succeeded {Succeeded}")]
    private partial void LogIngestionFinished(int read, int inserted, int updated, bool succeeded);

    [LoggerMessage(LogLevel.Information, "Loaded {Count} streets from the nomenclator")]
    private partial void LogStreets(int count);

    [LoggerMessage(LogLevel.Warning, "Skipping resource {Resource}")]
    private partial void LogResourceFailed(Exception ex, string resource);

    [LoggerMessage(LogLevel.Information, "Ingested {Resource}: {Rows} rows")]
    private partial void LogResource(string resource, int rows);
}
