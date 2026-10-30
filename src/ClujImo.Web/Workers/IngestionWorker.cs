using ClujImo.Infrastructure.Ingestion;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClujImo.Web.Workers;

/// <summary>Refreshes all datasets on start (or when the database is empty), on a schedule, and on demand.</summary>
public sealed class IngestionWorker(
    IServiceScopeFactory scopes,
    IngestionTrigger trigger,
    IOptions<OpenDataOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.RefreshOnStartup || await IsEmptyAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(options.Value.RefreshInterval);
            try
            {
                await trigger.WaitAsync(timeout.Token);
                trigger.Consume();
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // Scheduled refresh.
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IngestionService>().RunAsync(cancellationToken);
    }

    private async Task<bool> IsEmptyAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        return !await scope.ServiceProvider.GetRequiredService<AppDbContext>().Permits.AnyAsync(cancellationToken);
    }
}
