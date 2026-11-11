using System.Text;
using AskMyDocs.Api.Infrastructure.AI;
using AskMyDocs.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AskMyDocs.Api.Features.Ask;

public sealed record AskRequest(string Question, Guid[]? DocumentIds = null, int? TopK = null);

public sealed record AskSource(
    int Index,
    Guid DocumentId,
    string FileName,
    int PageNumber,
    string Excerpt,
    double Similarity);

public sealed record AskResponse(string Answer, IReadOnlyList<AskSource> Sources);

public static class AskEndpoints
{
    private const int DefaultTopK = 5;
    private const int MaxTopK = 20;
    private const int MaxQuestionLength = 2000;

    private const string SystemPrompt = """
        You are AskMyDocs, an assistant that answers questions using only the numbered
        excerpts provided from the user's documents.

        Rules:
        - Base your answer exclusively on the excerpts. Do not use outside knowledge.
        - Cite the excerpts you used by their number in square brackets, e.g. [1] or [2][3].
        - If the excerpts do not contain the answer, say clearly that you could not find it in the documents.
        - Answer in the same language as the question.
        """;

    private static readonly AskResponse NothingIndexed = new(
        "There are no indexed documents to search yet. Upload a PDF and wait until its status is Ready.",
        []);

    public static IEndpointRouteBuilder MapAskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/ask", Ask)
            .WithTags("Ask")
            .WithSummary("Ask a question about the uploaded documents.");

        return app;
    }

    private static async Task<Results<Ok<AskResponse>, ValidationProblem>> Ask(
        AskRequest request,
        AppDbContext db,
        IEmbeddingService embeddings,
        IChatService chat,
        CancellationToken cancellationToken)
    {
        var question = request.Question?.Trim() ?? string.Empty;
        var topK = request.TopK ?? DefaultTopK;

        var errors = new Dictionary<string, string[]>();
        if (question.Length == 0)
        {
            errors["question"] = ["The question is required."];
        }
        else if (question.Length > MaxQuestionLength)
        {
            errors["question"] = [$"The question must have at most {MaxQuestionLength} characters."];
        }

        if (topK is < 1 or > MaxTopK)
        {
            errors["topK"] = [$"topK must be between 1 and {MaxTopK}."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var chunks = db.Chunks.AsNoTracking();
        if (request.DocumentIds is { Length: > 0 } documentIds)
        {
            chunks = chunks.Where(c => documentIds.Contains(c.DocumentId));
        }

        // Skip the (paid) embedding call when there is nothing to search.
        if (!await chunks.AnyAsync(cancellationToken))
        {
            return TypedResults.Ok(NothingIndexed);
        }

        var questionVector = new Vector(await embeddings.EmbedAsync(question, cancellationToken));

        var hits = await chunks
            .Select(c => new
            {
                c.DocumentId,
                c.PageNumber,
                c.Content,
                Distance = c.Embedding.CosineDistance(questionVector)
            })
            .OrderBy(c => c.Distance)
            .Take(topK)
            .ToListAsync(cancellationToken);

        var hitDocumentIds = hits.Select(h => h.DocumentId).Distinct().ToList();
        var fileNames = await db.Documents
            .Where(d => hitDocumentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.FileName, cancellationToken);

        var sources = hits
            .Select((hit, i) => new AskSource(
                Index: i + 1,
                hit.DocumentId,
                fileNames.GetValueOrDefault(hit.DocumentId, "unknown"),
                hit.PageNumber,
                hit.Content,
                Similarity: Math.Round(1 - hit.Distance, 4)))
            .ToList();

        var answer = await chat.CompleteAsync(SystemPrompt, BuildUserPrompt(question, sources), cancellationToken);

        return TypedResults.Ok(new AskResponse(answer, sources));
    }

    private static string BuildUserPrompt(string question, IReadOnlyList<AskSource> sources)
    {
        var prompt = new StringBuilder("Excerpts:\n\n");

        foreach (var source in sources)
        {
            prompt.AppendLine($"[{source.Index}] ({source.FileName}, page {source.PageNumber})");
            prompt.AppendLine(source.Excerpt);
            prompt.AppendLine();
        }

        prompt.Append("Question: ").Append(question);
        return prompt.ToString();
    }
}
