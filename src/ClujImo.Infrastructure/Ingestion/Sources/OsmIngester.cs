using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Sources.Osm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure.Ingestion.Sources;

/// <summary>Addresses, neighborhood shapes and construction sites from OpenStreetMap. Each run replaces the previous snapshot.</summary>
public sealed class OsmIngester(AppDbContext db, OverpassClient overpass, IOptions<OsmOptions> options) : ISourceIngester
{
    public string Name => "osm";

    public bool Enabled => options.Value.Enabled;

    public TimeSpan RefreshInterval => options.Value.RefreshInterval;

    public async Task IngestAsync(IngestionRun run, CancellationToken cancellationToken)
    {
        var area = $"[out:json][timeout:180];area[\"name\"=\"{options.Value.AreaName}\"][\"boundary\"=\"administrative\"]->.a;";

        using var addressJson = await overpass.QueryAsync(area + "nwr(area.a)[\"addr:housenumber\"][\"addr:street\"];out tags center qt;", cancellationToken).ConfigureAwait(false);
        var addresses = OsmParser.Addresses(addressJson);

        using var placesJson = await overpass.QueryAsync(area + "nwr(area.a)[\"place\"~\"^(suburb|quarter|neighbourhood)$\"];out tags geom;", cancellationToken).ConfigureAwait(false);
        var neighborhoods = OsmParser.Neighborhoods(placesJson);

        using var sitesJson = await overpass.QueryAsync(area + "(nwr(area.a)[\"landuse\"=\"construction\"];nwr(area.a)[\"building\"=\"construction\"];);out tags center;", cancellationToken).ConfigureAwait(false);
        var sites = OsmParser.ConstructionSites(sitesJson);

        // Only replace when the snapshot looks sane: a failed query must not wipe good data.
        if (addresses.Count < 1000 || neighborhoods.Count < 5)
        {
            throw new InvalidOperationException($"OSM snapshot too small ({addresses.Count} addresses, {neighborhoods.Count} neighborhoods).");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await db.OsmAddresses.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.Neighborhoods.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.ConstructionSites.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        db.OsmAddresses.AddRange(addresses);
        db.Neighborhoods.AddRange(neighborhoods);
        db.ConstructionSites.AddRange(sites);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        run.RowsRead = addresses.Count + neighborhoods.Count + sites.Count;
        run.RowsInserted = run.RowsRead;
    }
}
