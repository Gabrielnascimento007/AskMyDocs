namespace AskMyDocs.Api.Infrastructure.AI;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; set; }
    public string ChatModel { get; set; } = "gpt-4.1-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}
