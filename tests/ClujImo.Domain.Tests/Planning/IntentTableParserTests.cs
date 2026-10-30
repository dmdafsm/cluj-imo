using ClujImo.Domain.Planning;

namespace ClujImo.Domain.Tests.Planning;

// Word positions mirror the April 2026 table: description column from x≈174, initiator column from x≈434,
// "Inițiator/Beneficiar" header at x=449. Initiators here are fictional.
public sealed class IntentTableParserTests
{
    private static readonly List<PdfWord> Words =
    [
        W(57, 717, "DOCUMENTAȚII"), W(156, 717, "PUD"),
        W(236, 688, "Denumire"), W(449, 688, "Inițiator/Beneficiar"),
        W(60, 660, "1"), W(90, 660, "449688/6.04.2026"), W(174, 660, "P.U.D."), W(206, 660, "Construire"), W(250, 660, "imobil"),
        W(279, 660, "mixt,"), W(389, 660, "str."), W(404, 660, "Piața"), W(434, 660, "EXEMPLU"), W(472, 660, "TRADING"), W(519, 660, "SRL"),
        W(174, 648, "Liebknecht"), W(222, 648, "nr.1"),
        W(60, 631, "2"), W(90, 631, "475766/21.04.2026"), W(174, 631, "P.U.D."), W(206, 631, "Desființare"), W(253, 631, "corpuri"),
        W(374, 631, "construire"), W(434, 631, "POPESCU"), W(484, 631, "ION"),
        W(174, 619, "imobil"), W(203, 619, "cu"), W(215, 619, "funcțiuni"), W(253, 619, "mixte,"), W(368, 619, "B-dul"), W(393, 619, "Muncii,"),
        W(57, 400, "DOCUMENTAȚII"), W(156, 400, "PUZ"),
        W(60, 370, "1"), W(90, 370, "458247/9.04.2026"), W(174, 370, "P.U.Z."), W(206, 370, "Dezvoltare"), W(250, 370, "urbană"),
        W(434, 370, "S.C."), W(460, 370, "EXEMPLU"), W(500, 370, "ESTATE"), W(530, 370, "S.R.L."),
    ];

    [Fact]
    public void Reads_rows_kinds_dates_and_columns()
    {
        var rows = IntentTableParser.Parse(Words);

        Assert.Equal(3, rows.Count);
        Assert.Equal(new IntentTableParser.Row("449688", new DateOnly(2026, 4, 6), UrbanPlanKind.Pud,
            "Construire imobil mixt, str. Piața Liebknecht nr.1", "EXEMPLU TRADING SRL"), rows[0]);
        Assert.Equal(UrbanPlanKind.Puz, rows[2].Kind);
        Assert.Equal("S.C. EXEMPLU ESTATE S.R.L.", rows[2].Initiator);
    }

    [Fact]
    public void Private_person_initiators_are_not_kept()
    {
        var rows = IntentTableParser.Parse(Words);

        Assert.Equal("Persoană fizică", rows[1].Initiator);
        Assert.Equal("Desființare corpuri construire imobil cu funcțiuni mixte, B-dul Muncii,", rows[1].Title);
    }

    private static PdfWord W(double x, double y, string text) => new(1, x, y, text);
}
