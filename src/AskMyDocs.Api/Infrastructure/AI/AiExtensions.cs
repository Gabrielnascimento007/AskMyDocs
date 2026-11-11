namespace AskMyDocs.Api.Infrastructure.AI;

public static class AiExtensions
{
    public static IServiceCollection AddOpenAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));

        services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddSingleton<IChatService, OpenAiChatService>();

        return services;
    }

    /// <summary>
    /// Resolves the API key from configuration (user secrets) or the OPENAI_API_KEY variable.
    /// Called lazily, on the first OpenAI request, so the API can start and serve
    /// non-AI endpoints even when the key has not been configured yet.
    /// </summary>
    internal static string GetApiKey(this OpenAiOptions options)
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
