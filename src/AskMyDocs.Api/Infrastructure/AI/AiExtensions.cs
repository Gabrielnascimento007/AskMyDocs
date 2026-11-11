using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace AskMyDocs.Api.Infrastructure.AI;

public static class AiExtensions
{
    public static IServiceCollection AddOpenAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));

        // Clients are created lazily so the API can start (and serve non-AI endpoints)
        // even when the key has not been configured yet.
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            return new EmbeddingClient(options.EmbeddingModel, GetApiKey(options));
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            return new ChatClient(options.ChatModel, GetApiKey(options));
        });

        services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddSingleton<IChatService, OpenAiChatService>();

        return services;
    }

    private static string GetApiKey(OpenAiOptions options)
    {
        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Run `dotnet user-secrets set \"OpenAI:ApiKey\" \"<your-key>\"` " +
                "in src/AskMyDocs.Api or set the OPENAI_API_KEY environment variable.");
        }

        return apiKey;
    }
}
