using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ClujImo.Infrastructure.Geocoding;

public sealed record GeocodeResult(double Latitude, double Longitude, string? Neighborhood);

/// <summary>Street-level geocoding through OpenStreetMap Nominatim, restricted to Cluj-Napoca.</summary>
public sealed class NominatimGeocoder(HttpClient http)
{
    // Cluj-Napoca bounding box (lon/lat): keeps namesakes in other cities out.
    private const string ViewBox = "23.45,46.85,23.75,46.70";

    public async Task<GeocodeResult?> GeocodeStreetAsync(string streetName, CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString($"{streetName}, Cluj-Napoca");
        var url = $"search?format=jsonv2&addressdetails=1&limit=1&countrycodes=ro&bounded=1&viewbox={ViewBox}&q={query}";
        var results = await http.GetFromJsonAsync<List<Place>>(url, cancellationToken).ConfigureAwait(false);
        var place = results?.FirstOrDefault();
        if (place is null
            || !double.TryParse(place.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !double.TryParse(place.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            return null;
        }

        var address = place.Address;
        var neighborhood = address?.Suburb ?? address?.Quarter ?? address?.Neighbourhood ?? address?.CityDistrict;
        return new GeocodeResult(lat, lon, neighborhood);
    }

    private sealed record Place(
        [property: JsonPropertyName("lat")] string? Lat,
        [property: JsonPropertyName("lon")] string? Lon,
        [property: JsonPropertyName("address")] PlaceAddress? Address);

    private sealed record PlaceAddress(
        [property: JsonPropertyName("suburb")] string? Suburb,
        [property: JsonPropertyName("quarter")] string? Quarter,
        [property: JsonPropertyName("neighbourhood")] string? Neighbourhood,
        [property: JsonPropertyName("city_district")] string? CityDistrict);
}
