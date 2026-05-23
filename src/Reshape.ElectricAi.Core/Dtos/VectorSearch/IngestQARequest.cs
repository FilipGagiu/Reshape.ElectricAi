using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record IngestQARequest(
    string QuestionText,
    IReadOnlyList<IngestAnswerRequest> Answers,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? QuestionCategoryValues = null);
