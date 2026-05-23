using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;

public sealed record FoodSectionData(
    IReadOnlyList<FoodRestriction> Restrictions,
    IReadOnlyList<Cuisine> PreferredCuisines,
    IReadOnlyList<RecommendedActivityDto> TopRestaurants);
