using System.Text.RegularExpressions;
using ClujImo.Domain.Text;

namespace ClujImo.Domain.Planning;

/// <summary>Sorts HCL titles into plan approvals, public investments and everything else.</summary>
public static partial class DecisionClassifier
{
    public static DecisionTopic Classify(string? title)
    {
        var text = TextNormalizer.ForMatching(title);
        if (PlanApproval().IsMatch(text) && !PlanNonApproval().IsMatch(text))
        {
            return DecisionTopic.UrbanPlanApproval;
        }

        return Investment().IsMatch(text) ? DecisionTopic.PublicInvestment : DecisionTopic.Other;
    }

    /// <summary>
    /// Splits "Aprobarea P.U.Z. – ...; beneficiari: Popescu Ion și Pop Ana." into the title and an initiator that is
    /// an organization name or "Persoană fizică". Private persons' names never leave this method.
    /// </summary>
    public static (string Title, string? Initiator) SplitBeneficiary(string title)
    {
        var match = Beneficiary().Match(title);
        if (!match.Success)
        {
            return (title.Trim(), null);
        }

        var who = match.Groups["who"].Value.Trim(' ', '.', ';');
        return (title[..match.Index].Trim(' ', ';', ',', '.') + ".", Parties.PartyName.Display(who));
    }

    public static UrbanPlanKind? PlanKind(string? title)
    {
        var match = Kind().Match(TextNormalizer.ForMatching(title));
        return !match.Success ? null : match.Groups[1].Value == "Z" ? UrbanPlanKind.Puz : UrbanPlanKind.Pud;
    }

    [GeneratedRegex(@"^APROBAREA? .*?(P\.?\s?U\.?\s?[ZD]\b|PLAN(ULUI)? URBANISTIC)")]
    private static partial Regex PlanApproval();

    // "Aprobarea prelungirii valabilității PUZ" or "revocarea" are not new plans.
    [GeneratedRegex(@"PRELUNGIR|VALABILITAT|REVOCAR|ABROGAR|INDREPTAR")]
    private static partial Regex PlanNonApproval();

    [GeneratedRegex(@"OBIECTIV\w* DE INVESTITI|INDICATORI\w* TEHNICO-ECONOMIC|DOCUMENTATI\w* TEHNICO-ECONOMIC|STUDIU\w* DE FEZABILITATE")]
    private static partial Regex Investment();

    [GeneratedRegex(@"P\.?\s?U\.?\s?([ZD])\b")]
    private static partial Regex Kind();

    [GeneratedRegex(@"[;,]?\s*(beneficiar[aiă]*|ini[tț]iator[i]*|solicitant[i]*)\s*:\s*(?<who>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex Beneficiary();
}
