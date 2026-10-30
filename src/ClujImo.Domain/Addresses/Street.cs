namespace ClujImo.Domain.Addresses;

/// <summary>A street from the municipal nomenclator, plus a cached geocode.</summary>
public sealed class Street
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? FormerName { get; set; }

    /// <summary>Fiscal zone A (central) … D (periphery), as set by the city.</summary>
    public string? FiscalZone { get; set; }

    /// <summary>Free-text area from the nomenclator ("Cartier Borhanci-sud", "PUZ Sopor ...").</summary>
    public string? Area { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    /// <summary>Neighborhood (suburb/quarter) reported by the geocoder.</summary>
    public string? Neighborhood { get; set; }

    public GeocodeStatus GeocodeStatus { get; set; }

    public DateTimeOffset? GeocodedAt { get; set; }
}

public enum GeocodeStatus
{
    Pending = 0,
    Found = 1,
    NotFound = 2,
}
