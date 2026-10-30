using System.Text.Json;
using ClujImo.Domain.Statistics;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Sources.Ins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure.Ingestion.Sources;

/// <summary>Yearly housing statistics for every locality of the county (INS TEMPO), for the metropolitan view.</summary>
public sealed class InsIngester(AppDbContext db, TempoClient tempo, IOptions<InsOptions> options) : ISourceIngester
{
    /// <summary>Matrix, how to pick the first (category) dimension, and which indicator each unit feeds.</summary>
    private static readonly (string Code, Func<string, bool> Category, Dictionary<string, MetroIndicator> Units)[] Matrices =
    [
        ("LOC104B", label => label == "Total", new() { ["Numar"] = MetroIndicator.DwellingsCompleted }),
        ("LOC108B", label => label.StartsWith("Cladiri rezidentiale (exclusiv", StringComparison.Ordinal), new()
        {
            ["Numar"] = MetroIndicator.ResidentialPermits,
            ["Metri patrati suprafata utila"] = MetroIndicator.ResidentialPermitArea,
        }),
        ("LOC101B", label => label == "Total", new() { ["Numar"] = MetroIndicator.DwellingStock }),
    ];

    private const int LocalitiesPerRequest = 20;

    public string Name => "ins";

    public bool Enabled => options.Value.Enabled;

    public TimeSpan RefreshInterval => options.Value.RefreshInterval;

    public async Task IngestAsync(IngestionRun run, CancellationToken cancellationToken)
    {
        var existing = await db.MetroStats.ToDictionaryAsync(s => (s.Indicator, s.Siruta, s.Year), cancellationToken).ConfigureAwait(false);
        foreach (var (code, category, units) in Matrices)
        {
            using var matrix = await tempo.GetMatrixAsync(code, cancellationToken).ConfigureAwait(false);
            var (selection, localityColumn) = Select(matrix, category);
            if (selection.Any(options => options.Count == 0))
            {
                throw new InvalidOperationException($"TEMPO matrix {code} changed: a dimension has no matching option.");
            }

            // TEMPO answers with an empty body when a request has too many cells: ask a few localities at a time.
            var values = new List<TempoValue>();
            foreach (var localities in selection[localityColumn].Chunk(LocalitiesPerRequest))
            {
                var chunk = selection.Select((options, i) => i == localityColumn ? localities : options).ToList();
                values.AddRange(TempoTable.Read(await tempo.GetDataSetAsync(code, matrix, chunk, cancellationToken).ConfigureAwait(false), localityColumn));
            }

            foreach (var value in values)
            {
                if (!units.TryGetValue(value.Unit, out var indicator))
                {
                    continue;
                }

                run.RowsRead++;
                if (existing.TryGetValue((indicator, value.Siruta, value.Year), out var stat))
                {
                    if (stat.Value != value.Value)
                    {
                        stat.Value = value.Value;
                        run.RowsUpdated++;
                    }
                }
                else
                {
                    stat = new MetroStat { Indicator = indicator, Siruta = value.Siruta, Locality = value.Locality, Year = value.Year, Value = value.Value };
                    db.MetroStats.Add(stat);
                    existing[(indicator, value.Siruta, value.Year)] = stat;
                    run.RowsInserted++;
                }
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Category by predicate, the county, all its localities, years from FromYear, every unit.</summary>
    private (IReadOnlyList<IReadOnlyList<JsonElement>> Selection, int LocalityColumn) Select(JsonDocument matrix, Func<string, bool> category)
    {
        var settings = options.Value;
        var dimensions = matrix.RootElement.GetProperty("dimensionsMap").EnumerateArray().ToList();
        var county = dimensions
            .Single(d => d.GetProperty("label").GetString()!.StartsWith("Judete", StringComparison.Ordinal))
            .GetProperty("options").EnumerateArray()
            .Single(o => o.GetProperty("label").GetString() == settings.County);
        var countyId = county.GetProperty("nomItemId").GetInt32();

        var selection = new List<IReadOnlyList<JsonElement>>();
        var localityColumn = -1;
        for (var i = 0; i < dimensions.Count; i++)
        {
            var label = dimensions[i].GetProperty("label").GetString()!.Trim();
            var options = dimensions[i].GetProperty("options").EnumerateArray().ToList();
            selection.Add(label switch
            {
                _ when label.StartsWith("Judete", StringComparison.Ordinal) => [county],
                _ when label.StartsWith("Localitati", StringComparison.Ordinal) => options
                    .Where(o => o.TryGetProperty("parentId", out var p) && p.ValueKind == JsonValueKind.Number && p.GetInt32() == countyId)
                    .ToList(),
                _ when label.StartsWith("Ani", StringComparison.Ordinal) => options
                    .Where(o => int.TryParse(o.GetProperty("label").GetString()![^4..], out var year) && year >= settings.FromYear)
                    .ToList(),
                _ when label.StartsWith("UM", StringComparison.Ordinal) => options,
                _ => options.Where(o => category(o.GetProperty("label").GetString()!.Trim())).ToList(),
            });

            if (label.StartsWith("Localitati", StringComparison.Ordinal))
            {
                localityColumn = i;
            }
        }

        return (selection, localityColumn);
    }
}
