using System.Text.RegularExpressions;
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Services.Itinerary;

internal static partial class EnumNaturalLanguage
{
    public static string ForEmbedding(FoodRestriction r) => r switch
    {
        FoodRestriction.Vegetarian => "vegetarian friendly",
        FoodRestriction.Vegan => "vegan friendly",
        FoodRestriction.NoGluten => "gluten free",
        FoodRestriction.NoDairy => "dairy free",
        FoodRestriction.NoMeat => "no meat",
        FoodRestriction.NoPork => "no pork",
        FoodRestriction.NoPeanuts => "no peanuts",
        FoodRestriction.NoShellfish => "no shellfish",
        FoodRestriction.NoEggs => "no eggs",
        FoodRestriction.Halal => "halal",
        FoodRestriction.Kosher => "kosher",
        _ => r.ToString().ToLowerInvariant()
    };

    public static string ForEmbedding(ActivityType a) =>
        PascalCaseSplitter().Replace(a.ToString(), "$1 $2").ToLowerInvariant();

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex PascalCaseSplitter();
}
