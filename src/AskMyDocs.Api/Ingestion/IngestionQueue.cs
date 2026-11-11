using System.Threading.Channels;

namespace AskMyDocs.Api.Ingestion;

public sealed record IngestionJob(Guid DocumentId, byte[] PdfBytes);

public sealed class IngestionQueue
{
    private readonly Channel<IngestionJob> _channel = Channel.CreateUnbounded<IngestionJob>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(IngestionJob job, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(job, cancellationToken);

    public IAsyncEnumerable<IngestionJob> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
