namespace ClujImo.Infrastructure.Persistence;

/// <summary>Large immutable files (SICAP quarters, monthly PDFs) already imported; they are not downloaded again.</summary>
public sealed class ProcessedResource
{
    public string Url { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; }

    public int Rows { get; set; }
}
