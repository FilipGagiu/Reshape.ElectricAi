namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record RetrievedChunk(
    Guid DocumentId,
    string DocumentTitle,
    int ChunkIndex,
    string Content,
    float Score);
