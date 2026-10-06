using Pgvector;

namespace AskMyDocs.Api.Domain;

public sealed class Chunk
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int PageNumber { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public Vector Embedding { get; private set; } = null!;

    private Chunk() { }

    public static Chunk Create(Guid documentId, int pageNumber, string content, float[] embedding) => new()
    {
        Id = Guid.NewGuid(),
        DocumentId = documentId,
        PageNumber = pageNumber,
        Content = content,
        Embedding = new Vector(embedding)
    };
}
