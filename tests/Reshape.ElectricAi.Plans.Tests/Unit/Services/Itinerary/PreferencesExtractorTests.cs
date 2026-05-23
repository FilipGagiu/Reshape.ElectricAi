using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Configuration;
using Reshape.ElectricAi.Plans.Services.Itinerary;
using Reshape.ElectricAi.Plans.Tests.Fakes;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services.Itinerary;

public class PreferencesExtractorTests
{
    private static PreferencesExtractor Build(FakeOpenAiClient fake) =>
        new(fake, Options.Create(new ItineraryGenerationOptions()), NullLogger<PreferencesExtractor>.Instance);

    [Fact]
    public async Task Extracts_canned_fields()
    {
        var envelope = new AiExtractedPreferences(
            Name: "Paul",
            Origin: "Cluj",
            Crew: new AiExtractedCrew(CrewKind.WithGroup, 4),
            VibeTags: ["full row", "party"],
            MusicGenres: null,
            MustSeeArtists: ["Teddy Swims"],
            FoodRestrictions: [FoodRestriction.Vegetarian],
            Cuisines: [Cuisine.Italian],
            ActivityInterests: null,
            SuggestedTransport: new AiExtractedTransportSuggestion(TransportMode.Car, null),
            SuggestedAccommodation: new AiExtractedAccommodationSuggestion(Accommodation.Camping, null),
            TicketType: null,
            AgeGroup: null);

        var fake = new FakeOpenAiClient().WithEnvelope(envelope);
        var extractor = Build(fake);

        var result = await extractor.ExtractAsync(
            answers: [new WizardAnswer("Q", "A", null)],
            freeText: null,
            locale: "en",
            cancellationToken: CancellationToken.None);

        Assert.Equal("Paul", result.Name);
        Assert.Equal("Cluj", result.Origin);
        Assert.Equal(CrewKind.WithGroup, result.Crew!.Kind);
        Assert.Equal(4, result.Crew.EstimatedSize);
        Assert.Equal(["Teddy Swims"], result.MustSeeArtists);
        Assert.Equal([FoodRestriction.Vegetarian], result.FoodRestrictions);
        Assert.Equal([Cuisine.Italian], result.Cuisines);
        Assert.Equal(TransportMode.Car, result.SuggestedTransport!.Mode);
        Assert.Equal(Accommodation.Camping, result.SuggestedAccommodation!.Type);
        Assert.Equal(1, fake.CallCount);
    }

    [Fact]
    public async Task Schema_violation_terminal()
    {
        var fake = new FakeOpenAiClient().WithException(new LlmSchemaException("preferences"));
        var extractor = Build(fake);

        await Assert.ThrowsAsync<LlmSchemaException>(() => extractor.ExtractAsync(
            answers: [],
            freeText: "anything",
            locale: "en",
            cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task Locale_propagates_into_user_prompt()
    {
        var envelope = new AiExtractedPreferences(
            null, null, null, null, null, null, null, null, null, null, null, null, null);
        var fake = new FakeOpenAiClient().WithEnvelope(envelope);
        var extractor = Build(fake);

        await extractor.ExtractAsync(
            answers: [new WizardAnswer("Cum te cheamă?", "Paul", null)],
            freeText: null,
            locale: "ro",
            cancellationToken: CancellationToken.None);

        Assert.Single(fake.Calls);
        Assert.Contains("locale=ro", fake.Calls[0].UserPrompt);
        Assert.Contains("Cum te cheamă?", fake.Calls[0].UserPrompt);
    }
}
