using AskMyDocs.Api.Domain;
using AskMyDocs.Api.Infrastructure.AI;
using AskMyDocs.Api.Infrastructure.Persistence;

namespace AskMyDocs.Api.Ingestion;

/// <summary>
/// Turns uploaded PDFs into embedded chunks: extract text per page, split it,
/// embed every chunk and persist them, then mark the document as Ready (or Failed).
/// </summary>
public sealed class IngestionWorker(
    IngestionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<IngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(job, stoppingToken);
        }
    }

    private async Task ProcessAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var embeddings = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        var document = await db.Documents.FindAsync([job.DocumentId], cancellationToken);
        if (document is null)
        {
            // Deleted before we got to it.
            return;
        }

        try
        {
            var pieces = PdfTextExtractor.ExtractPages(job.PdfBytes)
                .SelectMany(page => TextChunker.Split(page.Text)
                    .Select(content => (page.Number, Content: content)))
                .ToList();

            if (pieces.Count == 0)
            {
                throw new InvalidOperationException(
                    "No extractable text found in the PDF (it may be a scanned image).");
            }

            var vectors = await embeddings.EmbedBatchAsync(
                pieces.Select(p => p.Content).ToList(), cancellationToken);

            for (var i = 0; i < pieces.Count; i++)
            {
                db.Chunks.Add(Chunk.Create(document.Id, pieces[i].Number, pieces[i].Content, vectors[i]));
            }

            document.MarkReady();
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Document {DocumentId} ingested with {ChunkCount} chunks", document.Id, pieces.Count);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to ingest document {DocumentId}", document.Id);

            db.ChangeTracker.Clear();
            var failed = await db.Documents.FindAsync([job.DocumentId], cancellationToken);
            if (failed is not null)
            {
                failed.MarkFailed(ex.Message);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
