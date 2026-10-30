using ClujImo.Domain.Addresses;

namespace ClujImo.Domain.Tests.Addresses;

public sealed class StreetDirectoryTests
{
    private static readonly StreetDirectory Directory = new(
        new[] { "Strada Gheorghe Asachi", "Strada General Eremia Grigorescu", "Calea Turzii", "Strada Ion Agârbiceanu", "Strada Alexandru Vaida Voevod" }
            .Select(name => new Street { Key = StreetKey.From(name)!, Name = name }));

    [Theory]
    [InlineData("Calea Turzii", "Calea Turzii")]
    [InlineData("TURZII", "Calea Turzii")]
    [InlineData("G.Asachi", "Strada Gheorghe Asachi")]
    [InlineData("G-ral Eremia Grigorescu", "Strada General Eremia Grigorescu")]
    [InlineData("Agarbiceanu", "Strada Ion Agârbiceanu")]
    public void Resolves_spelling_variants(string written, string expected) =>
        Assert.Equal(expected, Directory.Resolve(StreetKey.From(written))?.Name);

    [Fact]
    public void Unknown_street_is_not_resolved() =>
        Assert.Null(Directory.Resolve(StreetKey.From("Strada Inexistentă")));
}
