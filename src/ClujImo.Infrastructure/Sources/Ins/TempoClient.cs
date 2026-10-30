using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClujImo.Infrastructure.Sources.Ins;

/// <summary>INS TEMPO API: matrix metadata and data sets for chosen dimension options.</summary>
public sealed class TempoClient(HttpClient http)
{
    public async Task<JsonDocument> GetMatrixAsync(string code, CancellationToken cancellationToken)
    {
        await using var stream = await http.GetStreamAsync($"matrix/{code}?lang=ro", cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Returns the table as rows of cells: 4 header rows (dimension, period, unit...) then data rows.</summary>
    public async Task<string[][]> GetDataSetAsync(string code, JsonDocument matrix, IReadOnlyList<IReadOnlyList<JsonElement>> selection, CancellationToken cancellationToken)
    {
        var request = new JsonObject
        {
            ["language"] = "ro",
            ["arr"] = new JsonArray([.. selection.Select(options => (JsonNode)new JsonArray([.. options.Select(o => (JsonNode)new JsonObject
            {
                ["label"] = o.GetProperty("label").GetString(),
                ["nomItemId"] = o.GetProperty("nomItemId").GetInt32(),
                ["offset"] = o.GetProperty("offset").GetInt32(),
                ["parentId"] = o.TryGetProperty("parentId", out var parent) && parent.ValueKind == JsonValueKind.Number ? parent.GetInt32() : null,
            })]))]),
            ["matrixName"] = matrix.RootElement.GetProperty("matrixName").GetString(),
            ["matrixDetails"] = JsonNode.Parse(matrix.RootElement.GetProperty("details").GetRawText()),
        };

        using var response = await http.PostAsJsonAsync($"matrix/dataSet/{code}", request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<string[][]>(cancellationToken).ConfigureAwait(false) ?? [];
    }
}
