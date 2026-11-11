using AskMyDocs.Api.Infrastructure.Persistence.Configurations;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace AskMyDocs.Api.Infrastructure.AI;

public sealed class OpenAiEmbeddingService(IOptions<OpenAiOptions> options) : IEmbeddingService
{
    // Well below OpenAI's per-request limits (2048 inputs / 300k tokens).
    private const int BatchSize = 64;

    private static readonly EmbeddingGenerationOptions GenerationOptions = new()
    {
        Dimensions = ChunkConfiguration.EmbeddingDimensions
    };

    private readonly Lazy<EmbeddingClient> _client = new(() =>
        new EmbeddingClient(options.Value.EmbeddingModel, options.Value.GetApiKey()));

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await _client.Value.GenerateEmbeddingAsync(text, GenerationOptions, cancellationToken);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = new List<float[]>(texts.Count);

        foreach (var batch in texts.Chunk(BatchSize))
        {
            var result = await _client.Value.GenerateEmbeddingsAsync(batch, GenerationOptions, cancellationToken);
            embeddings.AddRange(result.Value
                .OrderBy(e => e.Index)
                .Select(e => e.ToFloats().ToArray()));
        }

        return embeddings;
    }
}
