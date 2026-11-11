using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace AskMyDocs.Api.Infrastructure.AI;

/// <summary>Chat completions through any OpenAI-compatible API (Gemini, OpenAI, ...).</summary>
public sealed class OpenAiChatService(IOptions<AiOptions> options) : IChatService
{
    private readonly Lazy<ChatClient> _client = new(() =>
    {
        var (credential, clientOptions) = options.Value.CreateClientSettings();
        return new ChatClient(options.Value.ChatModel, credential, clientOptions);
    });

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        ChatMessage[] messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        ];

        var result = await _client.Value.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return string.Concat(result.Value.Content.Select(part => part.Text));
    }
}
