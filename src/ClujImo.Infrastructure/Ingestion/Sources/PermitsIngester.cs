using ClujImo.Domain.Permits;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure.Ingestion.Sources;

/// <summary>Building permits, urbanism and completion certificates, and the street nomenclator (data.gov.ro).</summary>
public sealed partial class PermitsIngester(
    AppDbContext db,
    CkanClient ckan,
    IOptions<OpenDataOptions> options,
    ILogger<PermitsIngester> logger) : ISourceIngester
{
    public string Name => "permits";

    public bool Enabled => true;

    public TimeSpan RefreshInterval => options.Value.RefreshInterval;

    public async Task IngestAsync(IngestionRun run, CancellationToken cancellationToken)
    {
        await IngestStreetsAsync(cancellationToken).ConfigureAwait(false);

        var settings = options.Value;
        await IngestPermitsAsync(PermitKind.BuildingPermit, settings.BuildingPermitsDataset, run, cancellationToken).ConfigureAwait(false);
        await IngestPermitsAsync(PermitKind.UrbanismCertificate, settings.UrbanismCertificatesDataset, run, cancellationToken).ConfigureAwait(false);
        await IngestPermitsAsync(PermitKind.CompletionCertificate, settings.CompletionCertificatesDataset, run, cancellationToken).ConfigureAwait(false);
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

    [LoggerMessage(LogLevel.Information, "Loaded {Count} streets from the nomenclator")]
    private partial void LogStreets(int count);

    [LoggerMessage(LogLevel.Warning, "Skipping resource {Resource}")]
    private partial void LogResourceFailed(Exception ex, string resource);

    [LoggerMessage(LogLevel.Information, "Ingested {Resource}: {Rows} rows")]
    private partial void LogResource(string resource, int rows);
}
