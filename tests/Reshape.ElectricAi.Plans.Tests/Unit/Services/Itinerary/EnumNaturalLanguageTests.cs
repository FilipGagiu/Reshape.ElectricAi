using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Services.Itinerary;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services.Itinerary;

public class EnumNaturalLanguageTests
{
    [Theory]
    [InlineData(FoodRestriction.Vegetarian, "vegetarian friendly")]
    [InlineData(FoodRestriction.Vegan, "vegan friendly")]
    [InlineData(FoodRestriction.NoGluten, "gluten free")]
    [InlineData(FoodRestriction.NoDairy, "dairy free")]
    [InlineData(FoodRestriction.NoMeat, "no meat")]
    [InlineData(FoodRestriction.NoPork, "no pork")]
    [InlineData(FoodRestriction.NoPeanuts, "no peanuts")]
    [InlineData(FoodRestriction.NoShellfish, "no shellfish")]
    [InlineData(FoodRestriction.NoEggs, "no eggs")]
    [InlineData(FoodRestriction.Halal, "halal")]
    [InlineData(FoodRestriction.Kosher, "kosher")]
    public void FoodRestriction_to_text(FoodRestriction r, string expected)
    {
        Assert.Equal(expected, EnumNaturalLanguage.ForEmbedding(r));
    }

    [Theory]
    [InlineData(ActivityType.Relax, "relax")]
    [InlineData(ActivityType.Energetic, "energetic")]
    [InlineData(ActivityType.Adrenaline, "adrenaline")]
    [InlineData(ActivityType.Social, "social")]
    [InlineData(ActivityType.Creative, "creative")]
    [InlineData(ActivityType.Wellness, "wellness")]
    [InlineData(ActivityType.Discovery, "discovery")]
    public void ActivityType_to_text(ActivityType a, string expected)
    {
        Assert.Equal(expected, EnumNaturalLanguage.ForEmbedding(a));
    }
}
