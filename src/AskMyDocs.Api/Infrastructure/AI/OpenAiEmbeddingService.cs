using AskMyDocs.Api.Infrastructure.Persistence.Configurations;
using OpenAI.Embeddings;

namespace AskMyDocs.Api.Infrastructure.AI;

public sealed class OpenAiEmbeddingService(EmbeddingClient client) : IEmbeddingService
{
    // Well below OpenAI's per-request limits (2048 inputs / 300k tokens).
    private const int BatchSize = 64;

    private static readonly EmbeddingGenerationOptions Options = new()
    {
        Dimensions = ChunkConfiguration.EmbeddingDimensions
    };

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await client.GenerateEmbeddingAsync(text, Options, cancellationToken);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = new List<float[]>(texts.Count);

        foreach (var batch in texts.Chunk(BatchSize))
        {
            var result = await client.GenerateEmbeddingsAsync(batch, Options, cancellationToken);
            embeddings.AddRange(result.Value
                .OrderBy(e => e.Index)
                .Select(e => e.ToFloats().ToArray()));
        }

        return embeddings;
    }
}
