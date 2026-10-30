using ClujImo.Infrastructure.Ingestion;
using ClujImo.Infrastructure.Sources;
using Microsoft.Extensions.Options;

namespace ClujImo.Web.Workers;

/// <summary>
/// Checks hourly which sources are due (each has its own refresh interval) and runs them; the UI can force a full
/// refresh. On start, sources that never succeeded are due, so an empty database fills itself.
/// </summary>
public sealed class IngestionWorker(
    IServiceScopeFactory scopes,
    IngestionTrigger trigger,
    IOptions<OpenDataOptions> options) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var force = options.Value.RefreshOnStartup;
        while (!stoppingToken.IsCancellationRequested)
        {
            await using (var scope = scopes.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<IngestionService>().RunAsync(force, stoppingToken);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(CheckInterval);
            try
            {
                await trigger.WaitAsync(timeout.Token);
                trigger.Consume();
                force = true;
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                force = false;
            }
        }
    }
}
