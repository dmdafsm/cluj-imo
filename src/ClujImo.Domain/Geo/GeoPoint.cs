namespace ClujImo.Domain.Geo;

public readonly record struct GeoPoint(double Latitude, double Longitude)
{
    private const double EarthRadiusMeters = 6_371_000;

    public double DistanceMetersTo(GeoPoint other)
    {
        var dLat = ToRadians(other.Latitude - Latitude);
        var dLon = ToRadians(other.Longitude - Longitude);
        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
            + (Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(other.Latitude)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2));
        return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    public static GeoPoint Centroid(IReadOnlyCollection<GeoPoint> points) =>
        new(points.Average(p => p.Latitude), points.Average(p => p.Longitude));

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}

/// <summary>How exactly a record was placed on the map.</summary>
public enum GeoPrecision
{
    None = 0,

    /// <summary>Somewhere on the street (centroid of the street).</summary>
    Street = 1,

    /// <summary>The closest known house number on the same street.</summary>
    NearbyNumber = 2,

    /// <summary>The exact address exists in OpenStreetMap.</summary>
    Address = 3,
}
