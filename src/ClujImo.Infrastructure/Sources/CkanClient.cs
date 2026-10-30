using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ClujImo.Infrastructure.Sources;

public sealed record CkanResource(string Name, Uri Url, int? Year);

/// <summary>Minimal client for the data.gov.ro CKAN API: lists the downloadable resources of a dataset.</summary>
public sealed partial class CkanClient(HttpClient http)
{
    public async Task<IReadOnlyList<CkanResource>> GetResourcesAsync(string datasetId, CancellationToken cancellationToken)
    {
        var response = await http.GetFromJsonAsync<CkanResponse>(
            $"package_show?id={Uri.EscapeDataString(datasetId)}", cancellationToken).ConfigureAwait(false);

        if (response is not { Success: true, Result: not null })
        {
            throw new InvalidOperationException($"CKAN returned no result for dataset '{datasetId}'.");
        }

        return response.Result.Resources
            .Where(r => Uri.TryCreate(r.Url, UriKind.Absolute, out _))
            .Select(r =>
            {
                var year = YearPattern().Match(r.Name ?? string.Empty);
                return new CkanResource(r.Name ?? string.Empty, new Uri(r.Url!), year.Success ? int.Parse(year.Value, provider: null) : null);
            })
            .ToList();
    }

    public Task<byte[]> DownloadAsync(Uri url, CancellationToken cancellationToken) =>
        http.GetByteArrayAsync(url, cancellationToken);

    [GeneratedRegex(@"\b20\d{2}\b")]
    private static partial Regex YearPattern();

    private sealed record CkanResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("result")] CkanPackage? Result);

    private sealed record CkanPackage(
        [property: JsonPropertyName("resources")] IReadOnlyList<CkanResourceDto> Resources);

    private sealed record CkanResourceDto(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("url")] string? Url);
}
