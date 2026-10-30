namespace ClujImo.Infrastructure.Persistence;

public sealed class IngestionRun
{
    public int Id { get; set; }

    /// <summary>Which source ran: "permits", "osm", "ins", "planning", "payments", "sicap".</summary>
    public string Source { get; set; } = "permits";

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public bool Succeeded { get; set; }

    public int RowsRead { get; set; }

    public int RowsInserted { get; set; }

    public int RowsUpdated { get; set; }

    public string? Error { get; set; }
}
