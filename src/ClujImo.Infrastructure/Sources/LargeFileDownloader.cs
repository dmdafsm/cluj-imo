namespace ClujImo.Infrastructure.Sources;

/// <summary>Streams big files (SICAP exports reach ~300 MB) to a temporary file that is deleted when disposed.</summary>
public sealed class LargeFileDownloader(HttpClient http)
{
    public async Task<FileStream> DownloadAsync(Uri url, CancellationToken cancellationToken)
    {
        var file = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await body.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            file.Position = 0;
            return file;
        }
        catch
        {
            await file.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
