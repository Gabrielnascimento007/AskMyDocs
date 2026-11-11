using System.ClientModel;
using OpenAI;

namespace AskMyDocs.Api.Infrastructure.AI;

public static class AiExtensions
{
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddSingleton<IChatService, OpenAiChatService>();

        return services;
    }

    /// <summary>
    /// Builds the SDK credential and client options. Called lazily, on the first AI request,
    /// so the API can start and serve non-AI endpoints even when the key is not configured yet.
    /// </summary>
    internal static (ApiKeyCredential Credential, OpenAIClientOptions ClientOptions) CreateClientSettings(this AiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "A chave da API de IA não está configurada. Rode `dotnet user-secrets set \"AI:ApiKey\" \"<sua-chave>\"` em src/AskMyDocs.Api " +
                "(para o Gemini, crie uma chave gratuita em https://aistudio.google.com/apikey).");
        }

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            clientOptions.Endpoint = new Uri(options.Endpoint);
        }

        return (new ApiKeyCredential(options.ApiKey), clientOptions);
    }
}
