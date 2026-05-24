using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Ask;

public sealed record AskRequest(
    string QuestionText,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null);
