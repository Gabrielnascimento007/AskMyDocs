namespace AskMyDocs.Api.Domain;


public enum DocumentStatus
{
    Processing,
    Ready,
    Failed
}

public sealed class Document
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public List<Chunk> Chunks { get; private set; } = [];

    private Document() { }

    public static Document Create(string fileName) => new()
    {
        Id = Guid.NewGuid(),
        FileName = fileName,
        Status = DocumentStatus.Processing,
        CreatedAt = DateTime.Now,
    };

    public void MarkReady()
    {
        Status = DocumentStatus.Ready;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = DocumentStatus.Failed;
        ErrorMessage = errorMessage;
    } 
}
