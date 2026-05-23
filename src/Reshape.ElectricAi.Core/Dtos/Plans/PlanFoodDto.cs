namespace Reshape.ElectricAi.Core.Dtos.Plans;

public sealed record PlanFoodDto(
    string Name,
    string Cuisine,
    IReadOnlyList<string> AllergenFlags,
    string PriceRange);
