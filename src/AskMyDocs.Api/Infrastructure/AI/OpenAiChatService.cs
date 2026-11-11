using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace AskMyDocs.Api.Infrastructure.AI;

public sealed class OpenAiChatService(IOptions<OpenAiOptions> options) : IChatService
{
    private readonly Lazy<ChatClient> _client = new(() =>
        new ChatClient(options.Value.ChatModel, options.Value.GetApiKey()));

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
