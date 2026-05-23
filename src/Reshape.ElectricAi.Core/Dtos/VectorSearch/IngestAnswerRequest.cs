using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record IngestAnswerRequest(
    string AnswerText,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? CategoryValues = null);
