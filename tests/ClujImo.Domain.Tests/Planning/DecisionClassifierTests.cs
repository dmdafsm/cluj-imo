using ClujImo.Domain.Planning;

namespace ClujImo.Domain.Tests.Planning;

public sealed class DecisionClassifierTests
{
    [Theory]
    [InlineData("Aprobarea P.U.Z. – Zonă de locuințe colective, str. Badea Cârțan nr. 8A.", DecisionTopic.UrbanPlanApproval)]
    [InlineData("Aprobarea P.U.D. – construire hotel, S+P+4E+R, str. Traian Vuia nr. 155A prov.", DecisionTopic.UrbanPlanApproval)]
    [InlineData("Aprobarea prelungirii valabilității P.U.Z. – str. Borhanciului", DecisionTopic.Other)]
    [InlineData("Aprobarea Documentației tehnico-economice, a indicatorilor tehnico-economici și a cofinanțării pentru obiectivul de investiții „Extinderea rețelei electrice de distribuție în Municipiul Cluj-Napoca, str. Triaj”.", DecisionTopic.PublicInvestment)]
    [InlineData("Aprobarea constituirii Comisiei de Acord Unic la nivelul municipiului Cluj-Napoca.", DecisionTopic.Other)]
    public void Classifies_decision_titles(string title, DecisionTopic expected) =>
        Assert.Equal(expected, DecisionClassifier.Classify(title));

    [Theory]
    [InlineData("Aprobarea P.U.Z. – Zonă de locuințe colective, str. Badea Cârțan nr. 8A; beneficiari: Nume Prenume și Alt Nume.", "Persoană fizică")]
    [InlineData("Aprobarea P.U.Z. – Dezvoltare urbană, str. Fabricii; beneficiar: S.C. Exemplu Imobiliare S.R.L.", "S.C. Exemplu Imobiliare S.R.L")]
    public void Beneficiary_names_are_split_off_and_private_persons_hidden(string title, string initiator)
    {
        var (cleanTitle, who) = DecisionClassifier.SplitBeneficiary(title);

        Assert.DoesNotContain("beneficiar", cleanTitle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Nume", cleanTitle, StringComparison.Ordinal);
        Assert.Equal(initiator, who);
    }
}
