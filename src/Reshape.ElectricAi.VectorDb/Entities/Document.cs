namespace Reshape.ElectricAi.VectorDb.Entities;

public class Document
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string SourceHash { get; set; }
    public DateTimeOffset IngestedUtc { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = [];
}
