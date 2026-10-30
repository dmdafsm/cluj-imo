using ClujImo.Domain.Geo;
using ClujImo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClujImo.Infrastructure.Ingestion;

/// <summary>Rebuilds derived columns everywhere: after any ingestion, a classifier change or new geocodes.</summary>
public static class Enrichment
{
    public static async Task RunAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        await PlaceStreetsFromOsmAsync(db, cancellationToken).ConfigureAwait(false);

        var geo = await GeoContext.LoadAsync(db, cancellationToken).ConfigureAwait(false);
        foreach (var permit in await db.Permits.ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            PermitEnricher.Enrich(permit, geo);
        }

        foreach (var plan in await db.UrbanPlans.ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            PermitEnricher.Enrich(plan, geo);
        }

        foreach (var site in await db.ConstructionSites.ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            site.Neighborhood = geo.Neighborhoods.Locate(new GeoPoint(site.Latitude, site.Longitude));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
    }

    /// <summary>Streets with OSM addresses get their center from them, so Nominatim is only needed for the rest.</summary>
    private static async Task PlaceStreetsFromOsmAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var centers = await db.OsmAddresses.AsNoTracking()
            .GroupBy(a => a.StreetKey)
            .Select(g => new { g.Key, Latitude = g.Average(a => a.Latitude), Longitude = g.Average(a => a.Longitude) })
            .ToDictionaryAsync(x => x.Key, cancellationToken)
            .ConfigureAwait(false);
        var locator = new NeighborhoodLocator(await db.Neighborhoods.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false));

        foreach (var street in await db.Streets.ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            if (centers.TryGetValue(street.Key, out var center))
            {
                street.Latitude = center.Latitude;
                street.Longitude = center.Longitude;
                street.GeocodeStatus = Domain.Addresses.GeocodeStatus.Found;
            }

            if (street.Latitude is { } lat && street.Longitude is { } lon && locator.Locate(new GeoPoint(lat, lon)) is { } neighborhood)
            {
                street.Neighborhood = neighborhood;
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
