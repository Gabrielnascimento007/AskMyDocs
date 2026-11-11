using AskMyDocs.Api.Domain;
using AskMyDocs.Api.Infrastructure.Persistence;
using AskMyDocs.Api.Ingestion;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AskMyDocs.Api.Features.Documents;

public sealed record DocumentResponse(
    Guid Id,
    string FileName,
    DocumentStatus Status,
    string? ErrorMessage,
    int ChunkCount,
    DateTime CreatedAt);

public static class DocumentEndpoints
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/documents").WithTags("Documents");

        group.MapPost("/", Upload)
            .DisableAntiforgery()
            .WithSummary("Upload a PDF to be indexed. Processing happens in the background.");
        group.MapGet("/", List).WithSummary("List uploaded documents.");
        group.MapGet("/{id:guid}", GetById).WithSummary("Get a document and its processing status.");
        group.MapDelete("/{id:guid}", Delete).WithSummary("Delete a document and its chunks.");

        return app;
    }

    private static async Task<Results<Accepted<DocumentResponse>, ValidationProblem>> Upload(
        IFormFile file,
        AppDbContext db,
        IngestionQueue queue,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return FileError("The file is empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return FileError($"The file exceeds the {MaxFileSizeBytes / 1024 / 1024} MB limit.");
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        if (!IsPdf(bytes))
        {
            return FileError("Only PDF files are supported.");
        }

        var document = Document.Create(Path.GetFileName(file.FileName));
        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(new IngestionJob(document.Id, bytes), cancellationToken);

        return TypedResults.Accepted($"/documents/{document.Id}", ToResponse(document, chunkCount: 0));
    }

    private static async Task<Ok<List<DocumentResponse>>> List(AppDbContext db, CancellationToken cancellationToken)
    {
        var documents = await db.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentResponse(d.Id, d.FileName, d.Status, d.ErrorMessage, d.Chunks.Count, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(documents);
    }

    private static async Task<Results<Ok<DocumentResponse>, NotFound>> GetById(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var document = await db.Documents
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DocumentResponse(d.Id, d.FileName, d.Status, d.ErrorMessage, d.Chunks.Count, d.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return document is null ? TypedResults.NotFound() : TypedResults.Ok(document);
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // Chunks are removed by the ON DELETE CASCADE foreign key.
        var deleted = await db.Documents
            .Where(d => d.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    private static bool IsPdf(byte[] bytes) =>
        bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8);

    private static ValidationProblem FileError(string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [message] });

    private static DocumentResponse ToResponse(Document document, int chunkCount) =>
        new(document.Id, document.FileName, document.Status, document.ErrorMessage, chunkCount, document.CreatedAt);
}
