using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record DocumentSearchFilter(
    string QueryText,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null,
    int TopK = 6);
