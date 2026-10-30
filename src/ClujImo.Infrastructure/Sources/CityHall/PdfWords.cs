using ClujImo.Domain.Planning;
using UglyToad.PdfPig;

namespace ClujImo.Infrastructure.Sources.CityHall;

public static class PdfWords
{
    public static IReadOnlyList<PdfWord> Read(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return [.. document.GetPages().SelectMany(page => page.GetWords()
            .Select(w => new PdfWord(page.Number, w.BoundingBox.Left, w.BoundingBox.Bottom, w.Text)))];
    }
}
