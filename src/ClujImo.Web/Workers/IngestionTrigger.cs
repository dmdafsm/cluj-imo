using System.Threading.Channels;

namespace ClujImo.Web.Workers;

/// <summary>Lets the UI ask the ingestion worker for an immediate refresh.</summary>
public sealed class IngestionTrigger
{
    private readonly Channel<bool> _requests = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
    });

    public void Request() => _requests.Writer.TryWrite(true);

    public ValueTask<bool> WaitAsync(CancellationToken cancellationToken) => _requests.Reader.WaitToReadAsync(cancellationToken);

    public void Consume() => _requests.Reader.TryRead(out _);
}
