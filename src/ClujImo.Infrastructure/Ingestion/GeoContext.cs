using ClujImo.Domain.Addresses;
using ClujImo.Domain.Geo;
using ClujImo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClujImo.Infrastructure.Ingestion;

public sealed record Placement(Street? Street, string? StreetKey, string? StreetDisplay, GeoPoint? Point, GeoPrecision Precision, string? Neighborhood);

/// <summary>Everything needed to place a street + number on the map and in a neighborhood.</summary>
public sealed class GeoContext(StreetDirectory streets, AddressIndex addresses, NeighborhoodLocator neighborhoods)
{
    public StreetDirectory Streets { get; } = streets;

    public AddressIndex Addresses { get; } = addresses;

    public NeighborhoodLocator Neighborhoods { get; } = neighborhoods;

    public static async Task<GeoContext> LoadAsync(AppDbContext db, CancellationToken cancellationToken) => new(
        new StreetDirectory(await db.Streets.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false)),
        new AddressIndex(await db.OsmAddresses.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false)),
        new NeighborhoodLocator(await db.Neighborhoods.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false)));

    public Placement Place(ParsedAddress? address)
    {
        if (address?.StreetKey is null)
        {
            return new Placement(null, null, address?.Street, null, GeoPrecision.None, null);
        }

        var street = Streets.Resolve(address.StreetKey);
        var key = street?.Key ?? address.StreetKey;
        GeoPoint? point = null;
        var precision = GeoPrecision.None;

        // "zona str. X" only says "near X": never pretend to know the house.
        if (Addresses.Locate(key, address.IsZone ? null : address.HouseNumber) is { } located)
        {
            (point, precision) = located;
        }
        else if (street is { Latitude: { } lat, Longitude: { } lon })
        {
            (point, precision) = (new GeoPoint(lat, lon), GeoPrecision.Street);
        }

        var neighborhood = Neighborhoods.Locate(point) ?? street?.Neighborhood;
        return new Placement(street, key, street?.Name ?? address.Street, point, precision, neighborhood);
    }
}
