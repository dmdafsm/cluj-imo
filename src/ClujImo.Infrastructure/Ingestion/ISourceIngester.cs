using ClujImo.Infrastructure.Persistence;

namespace ClujImo.Infrastructure.Ingestion;

/// <summary>One external source. The orchestrator decides when it is due and records the run.</summary>
public interface ISourceIngester
{
    /// <summary>Short stable id stored with each run ("permits", "osm", ...).</summary>
    string Name { get; }

    bool Enabled { get; }

    TimeSpan RefreshInterval { get; }

    Task IngestAsync(IngestionRun run, CancellationToken cancellationToken);
}
