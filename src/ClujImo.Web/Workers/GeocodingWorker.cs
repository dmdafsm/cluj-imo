using ClujImo.Infrastructure.Geocoding;
using Microsoft.Extensions.Options;

namespace ClujImo.Web.Workers;

/// <summary>Geocodes pending streets in small batches; idles when there is nothing left.</summary>
public sealed class GeocodingWorker(IServiceScopeFactory scopes, IOptions<GeocodingOptions> options, TimeProvider time) : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        // Give the first ingestion a head start.
        await Task.Delay(TimeSpan.FromSeconds(20), time, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            int processed;
            await using (var scope = scopes.CreateAsyncScope())
            {
                processed = await scope.ServiceProvider.GetRequiredService<GeocodingService>().GeocodeBatchAsync(25, stoppingToken);
            }

            if (processed == 0)
            {
                await Task.Delay(IdleDelay, time, stoppingToken);
            }
        }
    }
}
