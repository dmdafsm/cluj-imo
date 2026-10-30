using System.Text.RegularExpressions;
using ClujImo.Domain.Text;

namespace ClujImo.Domain.Parties;

/// <summary>
/// Shows names of organizations and hides names of private persons. The sources are public, but this app
/// is about what gets built, not about who the individual applicants are.
/// </summary>
public static partial class PartyName
{
    public const string PrivatePerson = "Persoană fizică";

    public static bool IsOrganization(string? name) =>
        !string.IsNullOrWhiteSpace(name) && OrganizationMarker().IsMatch(TextNormalizer.ForMatching(name))
        && !SoleTrader().IsMatch(TextNormalizer.ForMatching(name));

    /// <summary>The name for organizations, <see cref="PrivatePerson"/> otherwise, null when empty.</summary>
    public static string? Display(string? name)
    {
        var cleaned = TextNormalizer.Clean(name);
        if (cleaned.Length == 0)
        {
            return null;
        }

        return IsOrganization(cleaned) ? cleaned : PrivatePerson;
    }

    [GeneratedRegex(@"\b(S\.?\s?R\.?\s?L|S\.?\s?A|S\.?\s?C|SRL|SA|SCS|SNC|LTD|GMBH|KFT|ASOCIATIA|ASOCIATIE|FUNDATIA|COMPANIA|COMPANIE|REGIA|SOCIETATEA|COOPERATIVA|UNIVERSITATEA|SPITALUL|MUNICIPIUL|PRIMARIA|CONSILIUL|JUDETUL|MINISTERUL|ORDINUL|OPERA|INSTITUTUL|BISERICA|PAROHIA|EPISCOPIA|MITROPOLIA|CLUBUL|CLUB|CAMERA|AGENTIA|AUTORITATEA|DIRECTIA|SERVICIUL|INSPECTORATUL|COLEGIUL|LICEUL|SCOALA|GRADINITA|TEATRUL|MUZEUL|BANCA|BANK|TREZORERIA|CANTINA|CENTRUL|COMPLEXUL|HOLDING|GROUP|INVEST|IMOBILIARE|CONSTRUCT\w*|DEVELOPMENT|ESTATE|RESIDENCE|CONSULT\w*|TRADING|ENGINEERING|SISTEM\w*|SYSTEMS|INDUSTR\w*|ENERG\w*|ROMANIA)\b")]
    private static partial Regex OrganizationMarker();

    /// <summary>PFA / II / IF are run by and named after a person.</summary>
    [GeneratedRegex(@"\b(P\.?\s?F\.?\s?A|I\.?\s?I|I\.?\s?F)\.?$|\bPERSOANA FIZICA AUTORIZATA\b|\bINTREPRINDERE (INDIVIDUALA|FAMILIALA)\b")]
    private static partial Regex SoleTrader();
}
