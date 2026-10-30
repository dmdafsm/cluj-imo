using ClujImo.Infrastructure.Sources;

namespace ClujImo.Infrastructure.Tests.Sources;

public sealed class NomenclatorParserTests
{
    [Fact]
    public void Parses_names_keys_and_fiscal_zone()
    {
        var streets = NomenclatorParser.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "nomenclator.json")));

        var first = streets[0];
        Assert.Equal("Aleea Albăstrelelor", first.Name);
        Assert.Equal("ALBASTRELELOR", first.Key);
        Assert.Equal("D", first.FiscalZone);
        Assert.Equal("Cartier Borhanci-sud", first.Area);
    }
}
