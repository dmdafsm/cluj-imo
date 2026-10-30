using ClujImo.Domain.Classification;

namespace ClujImo.Domain.Tests.Classification;

// Every description below is copied from the published datasets.
public sealed class WorkClassifierTests
{
    [Theory]
    [InlineData("CONSTRUIRE IMOBIL DE LOCUINTE COLECTIVE S+P+3E,\\nAMENAJARI EXTERIOARE SI ORGANIZARE DE SANTIER", WorkCategory.NewCollectiveResidential)]
    [InlineData("IMOBIL DE LOCUINȚE COLECTIVE S+P+3E, AMENAJĂRI EXTERIOARE,  <BR/>ÎMPREJMUIRE TEREN, ORGANIZARE DE ȘANTIER", WorkCategory.NewCollectiveResidential)]
    [InlineData("CONSTRUIRE DE IMOBIL CU FUNCTIUNI MIXTE - LOCUINTE COLECTIVE , SPATII COMERCIALE SI SPATII SERVICII LA PARTER, AMENAJARI EXTERIOARE", WorkCategory.NewMixedUse)]
    [InlineData("<br/>DESFIINTARE CONSTRUCTIE EXISTENTA,<br/> CONSTRUIRE ANSAMBLU MIXT 2S+P/2S+P+9E,<br/>IMPREJMUIRE", WorkCategory.NewMixedUse)]
    [InlineData("CONSTRUIRE LOCUINȚĂ SEMICOLECTIVĂ CU DOUĂ UNITĂȚI LOCATIVE D+P+Er, ÎMPREJMUIRE TEREN, RACORDURI ȘI BRANȘAMENTE", WorkCategory.NewSemiCollectiveResidential)]
    [InlineData("LOCUINTA SEMICOLECTIVA P+E+R CU DOUA UNITATI LOCATIVE, IMPREJMUIRE TEREN", WorkCategory.NewSemiCollectiveResidential)]
    [InlineData("CONSTRUIRE CASA UNIFAMILIALA S+P+E, IMPREJMUIRE SI AMENAJARI EXTERIOARE\\n", WorkCategory.NewSingleFamily)]
    [InlineData("LOCUINȚĂ UNIFAMILIALĂ S+P+E, REFACERE ÎMPREJMUIRE, AMENAJĂRI EXTERIOARE", WorkCategory.NewSingleFamily)]
    [InlineData("CONSTRUIRE HALĂ MULTIFUNCTIONALĂ ȘI DEPOZIT LOGISTIC, ÎMPREJMUIRE TEREN, \\r\\nAMENAJĂRI EXTERIOARE ȘI ORGANIZARE DE ȘANTIER", WorkCategory.NewIndustrial)]
    [InlineData("DESFIINȚARE CORP C1 , CONSTRUIRE HOTEL-APARTAMENT CU REGIM DE INALTIME P+3E+R", WorkCategory.NewCommercial)]
    [InlineData("MODIFICĂRI INTERIOARE APARTAMENT NR. 2 ÎN VEDEREA REALIZĂRII A DOUĂ UNITĂȚI INDIVIDUALE, CU MENȚINEREA FUNCȚIUNILOR EXISTENTE", WorkCategory.Renovation)]
    [InlineData("MODIFICARI INTERIOARE IN VEDEREA REALIZARII UNEI LOCUINTE SEMICOLECTIVE CU 2 UNITATI LOCATIVE", WorkCategory.Renovation)]
    [InlineData("EXTINDERE APARTAMENT 2 PRIN AMENAJARE MANSARDĂ ÎN VOLUMUL PODULUI EXISTENT - CORP C1", WorkCategory.Extension)]
    [InlineData("AMPLASARE FIRMĂ LUMINOASĂ PE FAȚADA IMOBILULUI \\r\\n", WorkCategory.Signage)]
    [InlineData("AMPLASARE TOALETĂ PUBLICĂ AUTOMATĂ, BRANȘAMENT ELECTRIC, BRANȘAMENT APA ȘI RACORD CANALIZARE", WorkCategory.Infrastructure)]
    [InlineData("ALIMENTARE CU ENERGIE ELECTRICA IMOBIL DE LOCUINTE COLECTIVE", WorkCategory.Infrastructure)]
    [InlineData("ÎMPREJMUIRE ÎN SISTEM PROVIZORIU SI DEMONTABIL", WorkCategory.SiteWorks)]
    [InlineData("CONTINUAREA LUCRARILOR DE CRESTERE A EFICIENTEI ENERGETICE IN CLADIRILE REZIDENTIALE, RESPECTIV A BLOCURILOR DE LOCUINTE", WorkCategory.Renovation)]
    public void Classifies_real_descriptions(string description, WorkCategory expected) =>
        Assert.Equal(expected, WorkClassifier.Classify(description).Category);

    [Theory]
    [InlineData("CONTINUARE DE LUCRARI FARA MODIFICARE DE SOLUTIE LA CASA UNIFAMILIALA S+P+E, IMPREJMUIRE SI AMENAJARI EXTERIOARE AUTORIZATE CU A.C. NR.117 DIN 01.02.2018", WorkCategory.NewSingleFamily)]
    [InlineData("MODIFICARE DE TEMĂ PE PARCURSUL EXECUTĂRII LUCRĂRILOR LA A.C. NR.1067 DIN 19.12.2023 EMISA PENTRU IMOBIL DE LOCUINȚE COLECTIVE D+P+2E , ÎMPREJMUIRE TEREN", WorkCategory.NewCollectiveResidential)]
    public void Continuations_keep_the_building_type_but_are_not_counted_as_new(string description, WorkCategory expected)
    {
        var result = WorkClassifier.Classify(description);

        Assert.Equal(expected, result.Category);
        Assert.False(result.IsNewBuild);
        Assert.True(result.IsContinuation || result.IsAmendment);
    }

    [Fact]
    public void New_building_with_demolition_is_new_build_and_flags_demolition()
    {
        var result = WorkClassifier.Classify("DEMOLARE CASA P, CONSTRUIRE CASA UNIFAMILIALA Sp+P+E, REFACERE IMPREJMUIRE");

        Assert.Equal(WorkCategory.NewSingleFamily, result.Category);
        Assert.True(result.IsNewBuild);
        Assert.True(result.IncludesDemolition);
    }

    [Theory]
    [InlineData("REGIM DE INALTIME S+P+4E", "S+P+4E", 5)]
    [InlineData("LOCUINTA D + P + E + M", "D+P+E+M", 3)]
    [InlineData("CONSTRUIRE ANSAMBLU MIXT 2S+P/2S+P+9E", "2S+P+9E", 10)]
    [InlineData("IMOBIL S+P+3E+2R", "S+P+3E+2R", 6)]
    [InlineData("LOCUINȚĂ UNIFAMILIALĂ, ÎMPREJMUIRE TEREN, Regim de înălțime: P+M", "P+M", 2)]
    [InlineData("CONSTRUIRE IMPREJMUIRE", null, null)]
    public void Extracts_tallest_height_regime(string description, string? regime, int? levels)
    {
        var result = WorkClassifier.Classify(description);

        Assert.Equal(regime, result.HeightRegime);
        Assert.Equal(levels, result.LevelsAboveGround);
    }

    [Theory]
    [InlineData("LOCUINTA COLECTIVA CU 6 UNITATI LOCATIVE", 6)]
    [InlineData("LOCUINȚĂ COLECTIVĂ MICĂ (TREI UNITĂȚI LOCATIVE)", 3)]
    [InlineData("LOCUINTA SEMICOLECTIVA CU DOUA UNITATI LOCATIVE", 2)]
    [InlineData("IMOBIL CU 24 APARTAMENTE", 24)]
    [InlineData("MODIFICARI INTERIOARE APARTAMENT NR. 24", null)]
    public void Extracts_unit_count(string description, int? units) =>
        Assert.Equal(units, WorkClassifier.Classify(description).Units);

    [Fact]
    public void Single_family_wording_with_many_units_is_collective() =>
        Assert.Equal(
            WorkCategory.NewCollectiveResidential,
            WorkClassifier.Classify("CONSTRUIRE LOCUINTA CU 8 APARTAMENTE S+P+3E").Category);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Array")]
    public void Unknown_or_empty_text_is_other(string? description) =>
        Assert.Equal(WorkCategory.Other, WorkClassifier.Classify(description).Category);
}
