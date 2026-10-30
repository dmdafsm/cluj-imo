using ClujImo.Domain.Geo;

namespace ClujImo.Domain.Addresses;

/// <summary>Locates street + house number against OpenStreetMap address points.</summary>
public sealed class AddressIndex
{
    /// <summary>A neighbouring number further away than this is not "nearby" any more.</summary>
    private const int MaxNumberGap = 10;

    private readonly Dictionary<string, StreetAddresses> _streets;

    public AddressIndex(IEnumerable<OsmAddress> addresses)
    {
        _streets = addresses
            .GroupBy(a => a.StreetKey, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => new StreetAddresses(g.ToList()), StringComparer.Ordinal);
    }

    public int StreetCount => _streets.Count;

    public GeoPoint? StreetCenter(string? streetKey) =>
        streetKey is not null && _streets.TryGetValue(streetKey, out var street) ? street.Center : null;

    public (GeoPoint Point, GeoPrecision Precision)? Locate(string? streetKey, string? houseNumber)
    {
        if (streetKey is null || !_streets.TryGetValue(streetKey, out var street))
        {
            return null;
        }

        var normalized = HouseNumber.Normalize(houseNumber);
        if (normalized is not null && street.ByNumber.TryGetValue(normalized, out var exact))
        {
            return (exact, GeoPrecision.Address);
        }

        if (HouseNumber.Numeric(normalized) is { } number)
        {
            if (street.ByNumber.TryGetValue(number.ToString(System.Globalization.CultureInfo.InvariantCulture), out var plain))
            {
                return (plain, GeoPrecision.Address);
            }

            // Same side of the street first (same parity), then the closest number overall.
            var nearest = street.Numbered
                .Select(a => (a.Point, Gap: Math.Abs(a.Number - number), SameSide: a.Number % 2 == number % 2))
                .Where(a => a.Gap <= MaxNumberGap)
                .OrderByDescending(a => a.SameSide).ThenBy(a => a.Gap)
                .FirstOrDefault();
            if (nearest.Gap > 0 || nearest.SameSide)
            {
                return (nearest.Point, GeoPrecision.NearbyNumber);
            }
        }

        return (street.Center, GeoPrecision.Street);
    }

    private sealed class StreetAddresses
    {
        public StreetAddresses(IReadOnlyList<OsmAddress> addresses)
        {
            ByNumber = new Dictionary<string, GeoPoint>(StringComparer.Ordinal);
            var numbered = new List<(int Number, GeoPoint Point)>();
            foreach (var address in addresses)
            {
                var point = new GeoPoint(address.Latitude, address.Longitude);
                if (HouseNumber.Normalize(address.HouseNumber) is { } normalized)
                {
                    ByNumber.TryAdd(normalized, point);
                }

                if (HouseNumber.Numeric(address.HouseNumber) is { } number)
                {
                    numbered.Add((number, point));
                }
            }

            Numbered = numbered;
            Center = GeoPoint.Centroid(addresses.Select(a => new GeoPoint(a.Latitude, a.Longitude)).ToList());
        }

        public Dictionary<string, GeoPoint> ByNumber { get; }

        public IReadOnlyList<(int Number, GeoPoint Point)> Numbered { get; }

        public GeoPoint Center { get; }
    }
}
