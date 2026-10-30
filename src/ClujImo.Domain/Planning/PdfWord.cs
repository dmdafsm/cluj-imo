namespace ClujImo.Domain.Planning;

/// <summary>A word on a PDF page with its position (PDF coordinates: y grows upwards).</summary>
public readonly record struct PdfWord(int Page, double Left, double Bottom, string Text);
