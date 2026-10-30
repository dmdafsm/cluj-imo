using ClujImo.Domain.Addresses;
using ClujImo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure.Geocoding;

/// <summary>Geocodes streets that appear in permits, most-used first, and copies neighborhoods onto permits.</summary>
public sealed partial class GeocodingService(
    AppDbContext db,
    NominatimGeocoder geocoder,
    IOptions<GeocodingOptions> options,
    TimeProvider time,
    ILogger<GeocodingService> logger)
{
    /// <returns>How many streets were processed in this batch.</returns>
    public async Task<int> GeocodeBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        var pendingKeys = await db.Permits
            .Where(p => p.StreetKey != null)
            .GroupBy(p => p.StreetKey!)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .Join(db.Streets.Where(s => s.GeocodeStatus == GeocodeStatus.Pending), g => g.Key, s => s.Key, (g, s) => g)
            .OrderByDescending(g => g.Count)
            .Select(g => g.Key)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var key in pendingKeys)
        {
            var street = await db.Streets.SingleAsync(s => s.Key == key, cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await geocoder.GeocodeStreetAsync(street.Name, cancellationToken).ConfigureAwait(false);
                street.GeocodeStatus = result is null ? GeocodeStatus.NotFound : GeocodeStatus.Found;
                street.Latitude = result?.Latitude;
                street.Longitude = result?.Longitude;
                street.Neighborhood = result?.Neighborhood;
                street.GeocodedAt = time.GetUtcNow();
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                if (result?.Neighborhood is { } neighborhood)
                {
                    await db.Permits.Where(p => p.StreetKey == key)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Neighborhood, neighborhood), cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (HttpRequestException ex)
            {
                LogGeocodeFailed(ex, street.Name);
                break;
            }

            await Task.Delay(options.Value.Delay, time, cancellationToken).ConfigureAwait(false);
        }

        return pendingKeys.Count;
    }

    [LoggerMessage(LogLevel.Warning, "Geocoding failed for {Street}; will retry later")]
    private partial void LogGeocodeFailed(Exception ex, string street);
}
