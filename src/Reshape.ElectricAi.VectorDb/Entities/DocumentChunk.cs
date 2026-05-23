using Pgvector;

namespace Reshape.ElectricAi.VectorDb.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public required string Content { get; set; }
    public required Vector Embedding { get; set; }
    public string[] CategoryTags { get; set; } = [];
    public int ChunkIndex { get; set; }
    public Document Document { get; set; } = null!;
}
