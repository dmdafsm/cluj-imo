using ClujImo.Domain.Addresses;
using ClujImo.Domain.Classification;
using ClujImo.Domain.Permits;
using ClujImo.Infrastructure.Ingestion;

namespace ClujImo.Infrastructure.Tests;

public sealed class EnricherTests
{
    private static readonly StreetDirectory Streets = new([
        new Street { Key = "ALBERT EINSTEIN", Name = "Strada Albert Einstein", Neighborhood = "Gheorgheni" },
        new Street { Key = "MUNCII", Name = "Bulevardul Muncii", Neighborhood = "Muncii" },
    ]);

    [Fact]
    public void Building_permit_gets_canonical_street_neighborhood_and_category()
    {
        var permit = new Permit
        {
            Kind = PermitKind.BuildingPermit,
            RawAddress = "judetul Cluj, Cluj-Napoca, Str BD.MUNCII, nr. 18",
            Description = "CONSTRUIRE HALĂ MULTIFUNCTIONALĂ ȘI DEPOZIT LOGISTIC",
        };

        PermitEnricher.Enrich(permit, Streets);

        Assert.Equal("Bulevardul Muncii", permit.StreetDisplay);
        Assert.Equal("18", permit.HouseNumber);
        Assert.Equal("Muncii", permit.Neighborhood);
        Assert.Equal(WorkCategory.NewIndustrial, permit.Category);
        Assert.True(permit.IsNewBuild);
    }

    [Theory]
    [InlineData("tr. Albert Einstein, nr. 16-18", "Gheorgheni", "Strada Albert Einstein")]
    [InlineData("zorilor", "Zorilor", null)]
    public void Completion_certificate_uses_address_or_neighborhood(string raw, string neighborhood, string? street)
    {
        var permit = new Permit { Kind = PermitKind.CompletionCertificate, RawAddress = raw };

        PermitEnricher.Enrich(permit, Streets);

        Assert.Equal(neighborhood, permit.Neighborhood);
        Assert.Equal(street, permit.StreetDisplay);
    }
}
