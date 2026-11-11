namespace AskMyDocs.Api.Infrastructure.AI;

/// <summary>
/// Settings for any provider that exposes an OpenAI-compatible API
/// (Google Gemini, OpenAI, Ollama, ...). Bound from the "AI" configuration section.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "AI";

    /// <summary>
    /// Base URL of the OpenAI-compatible API. Leave empty to use OpenAI itself.
    /// Gemini: https://generativelanguage.googleapis.com/v1beta/openai/
    /// </summary>
    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }
    public string ChatModel { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>
    /// Texts sent per embedding request. Kept small so one request stays well under
    /// Gemini's free tier limit of 30,000 embedding tokens per minute.
    /// </summary>
    public int EmbeddingBatchSize { get; set; } = 16;
}
