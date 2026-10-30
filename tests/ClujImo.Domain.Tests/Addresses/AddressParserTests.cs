using ClujImo.Domain.Addresses;

namespace ClujImo.Domain.Tests.Addresses;

public sealed class AddressParserTests
{
    [Theory]
    [InlineData("judetul CLUJ, municipiul CLUJ-NAPOCA, Str Măceșului, nr. 13-15", "Măceșului", "MACESULUI", "13-15", false)]
    [InlineData("judetul Cluj, municipiul Cluj-Napoca, Adjudului, nr. 4", "Adjudului", "ADJUDULUI", "4", false)]
    [InlineData("judetul CLUJ, CLUJ-NAPOCA, Str B-dul Muncii, nr. 69 A", "B-dul Muncii", "MUNCII", "69 A", false)]
    [InlineData("judetul CLUJ, municipiul CLUJ-NAPOCA, Str zona str. Grigore Moisil, nr. FN", "Grigore Moisil", "GRIGORE MOISIL", null, true)]
    [InlineData("judetul CLUJ, municipiul CLUJ-NAPOCA, Str /zona str. Eugen Ionesco, nr. -", "Eugen Ionesco", "EUGEN IONESCO", null, true)]
    [InlineData("judetul Cluj, municipiul Cluj-Napoca, Str zona strazii Dealul Fânațelor, nr. -", "Dealul Fânațelor", "DEALUL FANATELOR", null, true)]
    [InlineData("judetul Cluj, Cluj-Napoca, Str Louis Pasteur, nr. 4-6, prof. Gheorghe Marinescu, nr. 23", "Louis Pasteur", "LOUIS PASTEUR", "4-6", false)]
    [InlineData("judetul Cluj, G.Asachi, nr. 13A", "G.Asachi", "G ASACHI", "13A", false)]
    [InlineData("judetul CLUJ, CLUJ-NAPOCA, Str P-ta 1 Mai, nr. -", "P-ta 1 Mai", "1 MAI", null, false)]
    [InlineData("judetul CLUJ, CLUJ-NAPOCA, Str G-RAL FILIP AGRICOLA, nr. 3", "G-RAL FILIP AGRICOLA", "FILIP AGRICOLA", "3", false)]
    public void Parses_city_address_format(string raw, string street, string key, string? number, bool isZone)
    {
        var parsed = AddressParser.Parse(raw);

        Assert.Equal(street, parsed.Street);
        Assert.Equal(key, parsed.StreetKey);
        Assert.Equal(number, parsed.HouseNumber);
        Assert.Equal(isZone, parsed.IsZone);
    }

    [Theory]
    [InlineData("judetul Cluj")]
    [InlineData("judetul CLUJ, municipiul CLUJ-NAPOCA, Str -, nr. -")]
    [InlineData("")]
    public void Addresses_without_street_have_no_key(string raw) =>
        Assert.Null(AddressParser.Parse(raw).StreetKey);
}
