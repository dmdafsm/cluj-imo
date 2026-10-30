using ClujImo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClujImo.Infrastructure.Ingestion;

/// <summary>Runs every due source in order, records one run per source, then rebuilds derived data.</summary>
public sealed partial class IngestionService(
    AppDbContext db,
    IEnumerable<ISourceIngester> sources,
    TimeProvider time,
    ILogger<IngestionService> logger)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static bool IsRunning => Gate.CurrentCount == 0;

    /// <param name="force">Run every enabled source regardless of its refresh interval.</param>
    public async Task<bool> RunAsync(bool force, CancellationToken cancellationToken)
    {
        if (!await Gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            LogAlreadyRunning();
            return false;
        }

        try
        {
            var anyRan = false;
            foreach (var source in sources.Where(s => s.Enabled))
            {
                if (!force && !await IsDueAsync(source, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                anyRan |= await RunSourceAsync(source, cancellationToken).ConfigureAwait(false);
            }

            if (anyRan)
            {
                await Enrichment.RunAsync(db, cancellationToken).ConfigureAwait(false);
            }

            return anyRan;
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<bool> IsDueAsync(ISourceIngester source, CancellationToken cancellationToken)
    {
        var last = await db.IngestionRuns.AsNoTracking()
            .Where(r => r.Source == source.Name && r.Succeeded)
            .OrderByDescending(r => r.Id)
            .Select(r => (DateTimeOffset?)r.StartedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return last is null || time.GetUtcNow() - last.Value >= source.RefreshInterval;
    }

    private async Task<bool> RunSourceAsync(ISourceIngester source, CancellationToken cancellationToken)
    {
        var run = new IngestionRun { Source = source.Name, StartedAt = time.GetUtcNow() };
        db.IngestionRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await source.IngestAsync(run, cancellationToken).ConfigureAwait(false);
            run.Succeeded = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogSourceFailed(ex, source.Name);
            run.Error = ex.Message;
        }
        finally
        {
            // A failed source may leave half-tracked entities behind; the run row is saved on a clean tracker.
            db.ChangeTracker.Clear();
            run.FinishedAt = time.GetUtcNow();
            db.IngestionRuns.Update(run);
            await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            db.ChangeTracker.Clear();
        }

        LogSourceFinished(source.Name, run.RowsRead, run.RowsInserted, run.RowsUpdated, run.Succeeded);
        return run.Succeeded;
    }

    [LoggerMessage(LogLevel.Information, "Ingestion already running, skipping")]
    private partial void LogAlreadyRunning();

    [LoggerMessage(LogLevel.Error, "Source {Source} failed")]
    private partial void LogSourceFailed(Exception ex, string source);

    [LoggerMessage(LogLevel.Information, "Source {Source} finished: read {Read}, inserted {Inserted}, updated {Updated}, succeeded {Succeeded}")]
    private partial void LogSourceFinished(string source, int read, int inserted, int updated, bool succeeded);
}
