namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record RetrievedChunk(
    Guid DocumentId,
    string DocumentTitle,
    string Content,
    float Score);
