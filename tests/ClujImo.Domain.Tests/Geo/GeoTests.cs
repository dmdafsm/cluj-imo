using ClujImo.Domain.Addresses;
using ClujImo.Domain.Geo;
using ClujImo.Domain.Parties;

namespace ClujImo.Domain.Tests.Geo;

public sealed class GeoTests
{
    private static readonly NeighborhoodArea Square = new()
    {
        Name = "Square",
        CenterLatitude = 46.77,
        CenterLongitude = 23.62,
        RingsJson = NeighborhoodArea.SerializeRings([[new(46.76, 23.61), new(46.78, 23.61), new(46.78, 23.63), new(46.76, 23.63), new(46.76, 23.61)]]),
    };

    [Fact]
    public void Polygon_contains_inner_point_only()
    {
        Assert.True(Square.Contains(new GeoPoint(46.77, 23.62)));
        Assert.False(Square.Contains(new GeoPoint(46.79, 23.62)));
    }

    [Fact]
    public void Locator_prefers_polygon_then_nearby_center_then_nothing()
    {
        var locator = new NeighborhoodLocator([Square, new NeighborhoodArea { Name = "Point", CenterLatitude = 46.80, CenterLongitude = 23.62 }]);

        Assert.Equal("Square", locator.Locate(new GeoPoint(46.775, 23.625)));
        Assert.Equal("Point", locator.Locate(new GeoPoint(46.801, 23.621)));
        Assert.Null(locator.Locate(new GeoPoint(46.90, 23.90)));
        Assert.Null(locator.Locate(null));
    }

    [Theory]
    [InlineData("69 A", "69A", 69)]
    [InlineData("13-15", "13", 13)]
    [InlineData("178K-188S", "178K", 178)]
    [InlineData("FN", null, null)]
    public void House_numbers_normalize(string raw, string? normalized, int? numeric)
    {
        Assert.Equal(normalized, HouseNumber.Normalize(raw));
        Assert.Equal(numeric, HouseNumber.Numeric(raw));
    }

    [Theory]
    [InlineData("construire imobil cu funcțiuni mixte, str. Paris, nr.70-72-74", "Paris", "PARIS", "70")]
    [InlineData("Urbanizare, B-dul Muncii - nord", "Muncii", "MUNCII", null)]
    [InlineData("Construire locuință semicolectivă, str. Fierului, nr.7A", "Fierului", "FIERULUI", "7A")]
    [InlineData("Restructurare urbană, str. Iris, nr.7", "Iris", "IRIS", "7")]
    [InlineData("Desființare corp C3, amenajare parcare, str. Calea Turzii, nr.200", "Calea Turzii", "TURZII", "200")]
    public void Finds_street_in_free_text(string text, string street, string key, string? number)
    {
        var address = FreeTextAddress.Find(text)!;

        Assert.Equal(street, address.Street);
        Assert.Equal(key, address.StreetKey);
        Assert.Equal(number, address.HouseNumber);
    }

    [Theory]
    [InlineData("ZAMBO TRADING SRL", true)]
    [InlineData("S.C. EIFEL GROUP S.R.L.", true)]
    [InlineData("OPERA NAȚIONALĂ ROMÂNĂ", true)]
    [InlineData("ASOCIATIA METROPOLITANA DE TRA", true)]
    [InlineData("POPESCU ION", false)]
    [InlineData("POPESCU ION PFA", false)]
    [InlineData("MARIUS EXEMPLU și asociații", false)]
    public void Organizations_are_told_apart_from_people(string name, bool organization) =>
        Assert.Equal(organization, PartyName.IsOrganization(name));
}
