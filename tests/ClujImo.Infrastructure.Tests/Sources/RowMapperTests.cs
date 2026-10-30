using ClujImo.Domain.Permits;
using ClujImo.Infrastructure.Sources;

namespace ClujImo.Infrastructure.Tests.Sources;

public sealed class RowMapperTests
{
    private static IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows(string name) =>
        LenientJson.ReadRows(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name)));

    [Fact]
    public void Maps_building_permit()
    {
        var permit = RowMapper.Map(PermitKind.BuildingPermit, Rows("ac_2026.json")[0], 2026)!;

        Assert.Equal(PermitKind.BuildingPermit, permit.Kind);
        Assert.Equal(2026, permit.Year);
        Assert.Equal("48", permit.Number);
        Assert.Equal(new DateOnly(2026, 1, 29), permit.IssuedOn);
        Assert.Equal(new DateOnly(2025, 12, 15), permit.RequestedOn);
        Assert.Equal(12, permit.ValidityMonths);
        Assert.Equal("AMPLASARE FIRMĂ LUMINOASĂ PE FAȚADA IMOBILULUI", permit.Description);
        Assert.Equal(45, permit.ProcessingDays);
    }

    [Fact]
    public void Maps_urbanism_certificate() =>
        Assert.Equal(new DateOnly(2025, 1, 10), RowMapper.Map(PermitKind.UrbanismCertificate, Rows("cu_2025.json")[0], 2025)!.IssuedOn);

    [Fact]
    public void Maps_completion_certificate_with_referenced_permit()
    {
        var permit = RowMapper.Map(PermitKind.CompletionCertificate, Rows("atestare_2022_broken.json")[0], 2022)!;

        Assert.Equal("513", permit.Number);
        Assert.Equal("170", permit.ReferencedPermitNumber);
        Assert.Equal(new DateOnly(2021, 2, 15), permit.ReferencedPermitDate);
    }

    [Fact]
    public void Skips_rows_without_number() =>
        Assert.Null(RowMapper.Map(PermitKind.BuildingPermit, new Dictionary<string, string?> { ["an"] = "2025" }, 2025));
}
