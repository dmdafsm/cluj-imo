using ClujImo.Domain.Addresses;
using ClujImo.Domain.Classification;
using ClujImo.Domain.Geo;
using ClujImo.Domain.Permits;
using ClujImo.Domain.Planning;
using ClujImo.Infrastructure.Ingestion;

namespace ClujImo.Infrastructure.Tests;

public sealed class EnricherTests
{
    // A square "Gheorgheni" around Einstein street, and a point-only "Muncii".
    private static readonly GeoContext Geo = new(
        new StreetDirectory([
            new Street { Key = "ALBERT EINSTEIN", Name = "Strada Albert Einstein" },
            new Street { Key = "MUNCII", Name = "Bulevardul Muncii", Latitude = 46.78, Longitude = 23.65, Neighborhood = "Nominatim name" },
            new Street { Key = "PARIS", Name = "Strada Paris" },
        ]),
        new AddressIndex([
            new OsmAddress { StreetKey = "ALBERT EINSTEIN", HouseNumber = "16", Latitude = 46.7700, Longitude = 23.6200 },
            new OsmAddress { StreetKey = "ALBERT EINSTEIN", HouseNumber = "20", Latitude = 46.7702, Longitude = 23.6204 },
            new OsmAddress { StreetKey = "PARIS", HouseNumber = "70", Latitude = 46.7800, Longitude = 23.5800 },
        ]),
        new NeighborhoodLocator([
            new NeighborhoodArea
            {
                Name = "Gheorgheni",
                CenterLatitude = 46.770,
                CenterLongitude = 23.620,
                RingsJson = NeighborhoodArea.SerializeRings([[new(46.76, 23.61), new(46.78, 23.61), new(46.78, 23.63), new(46.76, 23.63), new(46.76, 23.61)]]),
            },
            new NeighborhoodArea { Name = "Muncii", CenterLatitude = 46.781, CenterLongitude = 23.651 },
        ]));

    [Fact]
    public void Building_permit_on_known_address_gets_exact_position_and_polygon_neighborhood()
    {
        var permit = new Permit
        {
            Kind = PermitKind.BuildingPermit,
            RawAddress = "judetul Cluj, Cluj-Napoca, Str Albert Einstein, nr. 16",
            Description = "CONSTRUIRE IMOBIL DE LOCUINTE COLECTIVE S+P+3E",
        };

        PermitEnricher.Enrich(permit, Geo);

        Assert.Equal("Strada Albert Einstein", permit.StreetDisplay);
        Assert.Equal(GeoPrecision.Address, permit.GeoPrecision);
        Assert.Equal(46.77, permit.Latitude!.Value, 3);
        Assert.Equal("Gheorgheni", permit.Neighborhood);
        Assert.Equal(WorkCategory.NewCollectiveResidential, permit.Category);
    }

    [Fact]
    public void Unknown_number_uses_the_closest_number_on_the_same_side()
    {
        var permit = new Permit { Kind = PermitKind.BuildingPermit, RawAddress = "judetul Cluj, Cluj-Napoca, Str Albert Einstein, nr. 18" };

        PermitEnricher.Enrich(permit, Geo);

        Assert.Equal(GeoPrecision.NearbyNumber, permit.GeoPrecision);
    }

    [Fact]
    public void Street_without_osm_addresses_falls_back_to_street_geocode_and_nearest_center()
    {
        var permit = new Permit { Kind = PermitKind.BuildingPermit, RawAddress = "judetul Cluj, Cluj-Napoca, Str BD.MUNCII, nr. 18" };

        PermitEnricher.Enrich(permit, Geo);

        Assert.Equal("Bulevardul Muncii", permit.StreetDisplay);
        Assert.Equal(GeoPrecision.Street, permit.GeoPrecision);
        Assert.Equal("Muncii", permit.Neighborhood);
    }

    [Fact]
    public void Zone_addresses_are_never_placed_at_a_house()
    {
        var permit = new Permit { Kind = PermitKind.BuildingPermit, RawAddress = "judetul CLUJ, municipiul CLUJ-NAPOCA, Str zona str. Albert Einstein, nr. 16" };

        PermitEnricher.Enrich(permit, Geo);

        Assert.Equal(GeoPrecision.Street, permit.GeoPrecision);
    }

    [Theory]
    [InlineData("tr. Albert Einstein, nr. 16-18", "Gheorgheni", "Strada Albert Einstein")]
    [InlineData("zorilor", "Zorilor", null)]
    public void Completion_certificate_uses_address_or_neighborhood(string raw, string neighborhood, string? street)
    {
        var permit = new Permit { Kind = PermitKind.CompletionCertificate, RawAddress = raw };

        PermitEnricher.Enrich(permit, Geo);

        Assert.Equal(neighborhood, permit.Neighborhood);
        Assert.Equal(street, permit.StreetDisplay);
    }

    [Fact]
    public void Urban_plan_is_located_from_its_title()
    {
        var plan = new UrbanPlan { Title = "Desființare construcții existente și construire imobil cu funcțiuni mixte, amenajări exterioare, str. Paris, nr.70-72-74" };

        PermitEnricher.Enrich(plan, Geo);

        Assert.Equal("Strada Paris", plan.StreetDisplay);
        Assert.Equal(GeoPrecision.Address, plan.GeoPrecision);
        Assert.Equal(WorkCategory.NewMixedUse, plan.Category);
    }
}
