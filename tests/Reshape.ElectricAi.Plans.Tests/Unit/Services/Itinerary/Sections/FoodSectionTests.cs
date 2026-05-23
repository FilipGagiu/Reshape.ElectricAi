using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Services.Itinerary.Sections;
using Reshape.ElectricAi.Plans.Tests.Fakes;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services.Itinerary.Sections;

public class FoodSectionTests
{
    private static UserPreferencesSnapshot Snap(IReadOnlyList<FoodRestriction> r, IReadOnlyList<Cuisine> c) =>
        new(Guid.NewGuid(), null, null, null, null,
            [], [], [], r, c, [], null, null, null, null, null, null);

    [Fact]
    public async Task Composes_query_from_restrictions_and_cuisines()
    {
        var vector = new FakeVectorSearchService
        {
            DocumentResults =
            [
                new RetrievedChunk(Guid.NewGuid(), "Veggie Place", 0, "All vegetarian", 0.95f),
            ]
        };
        var section = new FoodSection(vector);

        var result = await section.BuildAsync(
            Snap([FoodRestriction.Vegetarian], [Cuisine.Italian]),
            CancellationToken.None);

        Assert.Equal("food", result.Key);
        Assert.Equal(40, result.Order);
        var call = vector.DocumentCalls[0];
        Assert.Contains("vegetarian", call.QueryText);
        Assert.Contains("italian", call.QueryText);
        Assert.NotNull(call.UserContext);
        Assert.True(call.UserContext!.ContainsKey(Category.Food));

        var data = result.Data.AsObject();
        Assert.Single(data["restrictions"]!.AsArray());
        Assert.Single(data["preferredCuisines"]!.AsArray());
        Assert.Single(data["topRestaurants"]!.AsArray());
    }

    [Fact]
    public async Task Falls_back_to_default_query_when_empty()
    {
        var vector = new FakeVectorSearchService();
        var section = new FoodSection(vector);

        await section.BuildAsync(Snap([], []), CancellationToken.None);

        Assert.Equal("restaurant", vector.DocumentCalls[0].QueryText);
    }
}
