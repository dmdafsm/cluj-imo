using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure.Sources.Osm;

/// <summary>Runs Overpass QL queries, falling back to the next endpoint when one is busy.</summary>
public sealed partial class OverpassClient(HttpClient http, IOptions<OsmOptions> options, ILogger<OverpassClient> logger)
{
    public async Task<JsonDocument> QueryAsync(string query, CancellationToken cancellationToken)
    {
        Exception? last = null;
        foreach (var url in options.Value.OverpassUrls)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new FormUrlEncodedContent([new("data", query)]),
                    };
                    request.Headers.Accept.ParseAdd("application/json");
                    using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json")
                    {
                        var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken).ConfigureAwait(false);

                        // A timed-out query still answers 200, with a "remark" and partial or no elements.
                        if (document.RootElement.TryGetProperty("remark", out var remark)
                            && remark.GetString()?.Contains("error", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            last = new HttpRequestException($"Overpass {url}: {remark.GetString()}");
                            document.Dispose();
                        }
                        else
                        {
                            return document;
                        }
                    }
                    else
                    {
                        last = new HttpRequestException($"Overpass {url} answered {(int)response.StatusCode} ({response.Content.Headers.ContentType?.MediaType}).");
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
                {
                    last = ex;
                }

                LogRetry(url, attempt + 1, last?.Message);
                await Task.Delay(TimeSpan.FromSeconds(10 * (attempt + 1)), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new HttpRequestException("All Overpass endpoints failed.", last);
    }

    [LoggerMessage(LogLevel.Warning, "Overpass {Url} busy or failing, attempt {Attempt}: {Reason}")]
    private partial void LogRetry(string url, int attempt, string? reason);
}
