using System.Text.RegularExpressions;
using ClujImo.Domain.Text;

namespace ClujImo.Domain.Investments;

/// <summary>Flags payments for construction works (as opposed to salaries, subsidies, utilities...).</summary>
public static partial class PaymentClassifier
{
    public static bool IsWorks(string? description) =>
        Works().IsMatch(TextNormalizer.ForMatching(description)) && !NotWorks().IsMatch(TextNormalizer.ForMatching(description));

    [GeneratedRegex(@"\bLUCR\w*|\bEXECUTI\w* (LUCR|OBIECTIV)|MODERNIZ|REABILIT|\bCONSTRU|AMENAJ|EXTINDER|REPARATI\w* CAPITAL|INVESTITI|ASFALT|\bSF\b|\bPT\b|DIRIGENT")]
    private static partial Regex Works();

    [GeneratedRegex(@"SALAR|INDEMNIZ|SUBVENTI|BURS|AJUTOR|CHIRIE|ENERGIE ELECTRICA|GAZE|TELEFON|COMBUSTIBIL|IMPOZIT|CONTRIBUTI|RESTITU")]
    private static partial Regex NotWorks();
}
