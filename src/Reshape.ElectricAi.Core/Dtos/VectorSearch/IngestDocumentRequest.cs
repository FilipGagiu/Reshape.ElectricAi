using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record IngestDocumentRequest(
    string Title,
    string Content,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? CategoryValues = null);
