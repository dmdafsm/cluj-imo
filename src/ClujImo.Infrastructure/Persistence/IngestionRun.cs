namespace ClujImo.Infrastructure.Persistence;

public sealed class IngestionRun
{
    public int Id { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public bool Succeeded { get; set; }

    public int RowsRead { get; set; }

    public int RowsInserted { get; set; }

    public int RowsUpdated { get; set; }

    public string? Error { get; set; }
}
