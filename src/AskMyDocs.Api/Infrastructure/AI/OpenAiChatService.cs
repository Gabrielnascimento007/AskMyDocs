using OpenAI.Chat;

namespace AskMyDocs.Api.Infrastructure.AI;

public sealed class OpenAiChatService(ChatClient client) : IChatService
{
    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        ChatMessage[] messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        ];

        var result = await client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return string.Concat(result.Value.Content.Select(part => part.Text));
    }
}
