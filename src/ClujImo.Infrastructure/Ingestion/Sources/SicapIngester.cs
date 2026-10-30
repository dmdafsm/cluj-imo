using System.Globalization;
using System.Text.RegularExpressions;
using ClujImo.Domain.Investments;
using ClujImo.Domain.Text;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Sources;
using ClujImo.Infrastructure.Sources.Sicap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure.Ingestion.Sources;

/// <summary>
/// Public procurement contracts of the city (SICAP exports on data.gov.ro, one file per quarter). Files are large and
/// immutable once published, so each is imported once.
/// </summary>
public sealed partial class SicapIngester(
    AppDbContext db,
    CkanClient ckan,
    LargeFileDownloader downloader,
    IOptions<InvestmentOptions> options,
    TimeProvider time,
    ILogger<SicapIngester> logger) : ISourceIngester
{
    private static readonly string[] Readable = [".xlsx", ".xls", ".csv", ".ods"];

    public string Name => "sicap";

    public bool Enabled => options.Value.Enabled;

    public TimeSpan RefreshInterval => options.Value.SicapRefreshInterval;

    public async Task IngestAsync(IngestionRun run, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var cuis = settings.AuthorityCuis.ToHashSet(StringComparer.Ordinal);
        // A quarter without a single contract of the city means the file was not understood: read it again next time.
        var processed = await db.ProcessedResources.Where(r => r.Source == Name && r.Rows > 0).Select(r => r.Url).ToHashSetAsync(cancellationToken).ConfigureAwait(false);
        var known = (await db.PublicContracts.Select(c => new { c.ContractNumber, c.AwardNoticeNumber, c.Lot }).ToListAsync(cancellationToken).ConfigureAwait(false))
            .Select(c => (c.ContractNumber, c.AwardNoticeNumber, c.Lot))
            .ToHashSet();

        for (var year = settings.SicapFromYear; year <= time.GetUtcNow().Year; year++)
        {
            var dataset = string.Format(CultureInfo.InvariantCulture, settings.SicapDatasetPattern, year);
            var resources = await ckan.TryGetResourcesAsync(dataset, cancellationToken).ConfigureAwait(false);
            foreach (var resource in ContractFiles(resources ?? []).Where(r => !processed.Contains(r.Url.ToString())))
            {
                var rows = 0;
                try
                {
                    await using var file = await downloader.DownloadAsync(resource.Url, cancellationToken).ConfigureAwait(false);
                    foreach (var contract in SicapReader.Read(file, Extension(resource.Url), cuis))
                    {
                        rows++;
                        run.RowsRead++;
                        if (known.Add((contract.ContractNumber, contract.AwardNoticeNumber, contract.Lot)))
                        {
                            db.PublicContracts.Add(contract);
                            run.RowsInserted++;
                        }
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or FormatException
                    or ExcelDataReader.Exceptions.ExcelReaderException)
                {
                    LogFileFailed(ex, resource.Name);
                    continue;
                }

                var record = await db.ProcessedResources.FindAsync([resource.Url.ToString()], cancellationToken).ConfigureAwait(false);
                if (record is null)
                {
                    db.ProcessedResources.Add(new ProcessedResource { Url = resource.Url.ToString(), Source = Name, ProcessedAt = time.GetUtcNow(), Rows = rows });
                }
                else
                {
                    (record.ProcessedAt, record.Rows) = (time.GetUtcNow(), rows);
                }
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                db.ChangeTracker.Clear();
                LogFile(resource.Name, rows);
            }
        }
    }

    /// <summary>"Contracte ..." files we can read; a CSV is skipped when the same report also exists as Excel.</summary>
    internal static IEnumerable<CkanResource> ContractFiles(IEnumerable<CkanResource> resources)
    {
        var contracts = resources
            .Where(r => r.Name.Contains("ontract", StringComparison.OrdinalIgnoreCase)
                && !r.Name.Contains("subsecvent", StringComparison.OrdinalIgnoreCase)
                && !r.Name.Contains("modific", StringComparison.OrdinalIgnoreCase)
                && Readable.Contains(Extension(r.Url)))
            .ToList();
        var excel = contracts.Where(r => Extension(r.Url) != ".csv").Select(r => ReportKey(r.Name)).ToHashSet(StringComparer.Ordinal);
        return contracts.Where(r => Extension(r.Url) != ".csv" || !excel.Contains(ReportKey(r.Name)));
    }

    private static string Extension(Uri url) => Path.GetExtension(url.AbsolutePath).ToLowerInvariant();

    /// <summary>"Raport Contracte publicate T II 2024 .xls" and "... CSV" → same key.</summary>
    private static string ReportKey(string name) =>
        ReportNoise().Replace(TextNormalizer.ForMatching(name), string.Empty);

    [GeneratedRegex(@"RAPORT|PUBLICATE|\.?(CSV|XLSX|XLS)\b|[^A-Z0-9]")]
    private static partial Regex ReportNoise();

    [LoggerMessage(LogLevel.Warning, "Could not read SICAP file {Name}")]
    private partial void LogFileFailed(Exception ex, string name);

    [LoggerMessage(LogLevel.Information, "SICAP {Name}: {Rows} contracts of the selected authorities")]
    private partial void LogFile(string name, int rows);
}
