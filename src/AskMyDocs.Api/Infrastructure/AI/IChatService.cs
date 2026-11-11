namespace AskMyDocs.Api.Infrastructure.AI;

public interface IChatService
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
