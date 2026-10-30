using System.Text.RegularExpressions;

namespace ClujImo.Domain.Addresses;

/// <summary>
/// Parses the city's address field, e.g. "judetul CLUJ, municipiul CLUJ-NAPOCA, Str zona str. Grigore Moisil, nr. FN".
/// </summary>
public static partial class AddressParser
{
    public static ParsedAddress Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new ParsedAddress(null, null, null, false);
        }

        var numberMatch = NumberMarker().Match(raw);
        var location = numberMatch.Success ? raw[..numberMatch.Index] : raw;
        var houseNumber = numberMatch.Success ? CleanHouseNumber(raw[(numberMatch.Index + numberMatch.Length)..]) : null;

        var street = location.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault(part => !AdministrativePart().IsMatch(part));
        if (street is null)
        {
            return new ParsedAddress(null, null, houseNumber, false);
        }

        street = FormPrefix().Replace(street, string.Empty);
        var zone = ZonePrefix().Match(street);
        if (zone.Success)
        {
            street = street[zone.Length..];
        }

        street = street.Trim(' ', '-', '/', '.');
        if (street.Length == 0)
        {
            return new ParsedAddress(null, null, houseNumber, zone.Success);
        }

        return new ParsedAddress(street, StreetKey.From(street), houseNumber, zone.Success);
    }

    private static string? CleanHouseNumber(string value)
    {
        var number = value.Split(',', 2)[0].Trim();
        return number is "" or "-" || NoNumber().IsMatch(number) ? null : number;
    }

    [GeneratedRegex(@",\s*nr\.?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex NumberMarker();

    [GeneratedRegex(@"^(jude[tț]ul|jud\.?|municipiul|mun\.?|cluj-napoca$|cluj$)", RegexOptions.IgnoreCase)]
    private static partial Regex AdministrativePart();

    [GeneratedRegex(@"^\s*Str\.?\s+")]
    private static partial Regex FormPrefix();

    [GeneratedRegex(@"^[\s/]*(?:[iî]n\s+)?zona\s+(?:(?:str(?:ad[aă]|[aă]zii|\.)?|b-dul|bd\.?|calea)\s*)?", RegexOptions.IgnoreCase)]
    private static partial Regex ZonePrefix();

    [GeneratedRegex(@"^(F\.?\s*N\.?|FN|f\.?n\.?)$", RegexOptions.IgnoreCase)]
    private static partial Regex NoNumber();
}
