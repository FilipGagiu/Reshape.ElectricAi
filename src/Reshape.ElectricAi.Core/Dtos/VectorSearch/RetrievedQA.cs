namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record RetrievedQA(
    string QuestionText,
    IReadOnlyList<RetrievedAnswer> Answers,
    float QuestionScore);
