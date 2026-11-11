using AskMyDocs.Api.Infrastructure.Persistence.Configurations;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace AskMyDocs.Api.Infrastructure.AI;

/// <summary>Embeddings through any OpenAI-compatible API (Gemini, OpenAI, ...).</summary>
public sealed class OpenAiEmbeddingService(IOptions<AiOptions> options) : IEmbeddingService
{
    private static readonly EmbeddingGenerationOptions GenerationOptions = new()
    {
        Dimensions = ChunkConfiguration.EmbeddingDimensions
    };

    private readonly Lazy<EmbeddingClient> _client = new(() =>
    {
        var (credential, clientOptions) = options.Value.CreateClientSettings();
        return new EmbeddingClient(options.Value.EmbeddingModel, credential, clientOptions);
    });

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await _client.Value.GenerateEmbeddingAsync(text, GenerationOptions, cancellationToken);
        return EnsureDimensions(result.Value.ToFloats().ToArray());
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = new List<float[]>(texts.Count);

        foreach (var batch in texts.Chunk(Math.Max(1, options.Value.EmbeddingBatchSize)))
        {
            var result = await _client.Value.GenerateEmbeddingsAsync(batch, GenerationOptions, cancellationToken);
            embeddings.AddRange(result.Value
                .OrderBy(e => e.Index)
                .Select(e => EnsureDimensions(e.ToFloats().ToArray())));
        }

        return embeddings;
    }

    // The database column is vector(1536); fail with a clear message instead of a cryptic
    // PostgreSQL error if the provider ignores the requested dimensions.
    private float[] EnsureDimensions(float[] vector) =>
        vector.Length == ChunkConfiguration.EmbeddingDimensions
            ? vector
            : throw new InvalidOperationException(
                $"O modelo de embeddings '{options.Value.EmbeddingModel}' devolveu {vector.Length} dimensões, " +
                $"mas o banco espera {ChunkConfiguration.EmbeddingDimensions}.");
}
