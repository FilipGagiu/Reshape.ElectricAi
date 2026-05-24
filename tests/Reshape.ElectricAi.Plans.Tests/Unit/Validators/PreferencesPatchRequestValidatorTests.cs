using FluentValidation.TestHelper;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Validators;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Validators;

public sealed class PreferencesPatchRequestValidatorTests
{
    private readonly PreferencesPatchRequestValidator _validator = new();

    private static readonly PreferencesPatchRequest Empty = new(
        Name: null,
        Origin: null,
        Crew: null,
        VibeTags: null,
        MusicGenres: null,
        MustSeeArtists: null,
        FoodRestrictions: null,
        Cuisines: null,
        ActivityInterests: null,
        SuggestedTransport: null,
        SuggestedAccommodation: null,
        TicketType: null,
        AgeGroup: null);

    [Fact]
    public void Validate_AllSectionsSkipped_Passes()
    {
        var result = _validator.TestValidate(Empty);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CrewWithNullKindAndNullSize_Passes()
    {
        var req = Empty with { Crew = new CrewDto(Kind: null, EstimatedSize: null) };

        var result = _validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CrewWithNullKindAndValidSize_Passes()
    {
        var req = Empty with { Crew = new CrewDto(Kind: null, EstimatedSize: 5) };

        var result = _validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_SuggestedTransportWithNullMode_Passes()
    {
        var req = Empty with { SuggestedTransport = new TransportSuggestionDto(Mode: null, Note: "x") };

        var result = _validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_SuggestedAccommodationWithNullType_Passes()
    {
        var req = Empty with { SuggestedAccommodation = new AccommodationSuggestionDto(Type: null, Note: null) };

        var result = _validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_VibeTagsWithEmptyAndNullEntries_Passes()
    {
        var req = Empty with { VibeTags = new[] { "", null!, "ok" } };

        var result = _validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MustSeeArtistsWithEmptyEntry_Passes()
    {
        var req = Empty with { MustSeeArtists = new[] { "" } };

        var result = _validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CrewKindOutOfRange_Fails()
    {
        var req = Empty with { Crew = new CrewDto(Kind: (CrewKind)999, EstimatedSize: null) };

        var result = _validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.Crew!.Kind)
              .WithErrorMessage("Crew.Kind must be a valid value.");
    }

    [Fact]
    public void Validate_CrewEstimatedSizeBelowRange_Fails()
    {
        var req = Empty with { Crew = new CrewDto(Kind: null, EstimatedSize: 0) };

        var result = _validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.Crew!.EstimatedSize)
              .WithErrorMessage("Crew.EstimatedSize must be between 1 and 200.");
    }

    [Fact]
    public void Validate_VibeTagItemExceedsLength_Fails()
    {
        var tooLong = new string('a', 61);
        var req = Empty with { VibeTags = new[] { tooLong } };

        var result = _validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor("VibeTags[0]")
              .WithErrorMessage("VibeTag values must be 60 characters or fewer.");
    }

    [Fact]
    public void Validate_MustSeeArtistsItemExceedsLength_Fails()
    {
        var tooLong = new string('a', 201);
        var req = Empty with { MustSeeArtists = new[] { tooLong } };

        var result = _validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor("MustSeeArtists[0]")
              .WithErrorMessage("MustSeeArtists names must be 200 characters or fewer.");
    }

    [Fact]
    public void Validate_VibeTagsExceedsMaxCount_Fails()
    {
        var req = Empty with { VibeTags = new[] { "a", "b", "c", "d", "e", "f", "g" } };

        var result = _validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.VibeTags!.Count)
              .WithErrorMessage("VibeTags must contain at most 6 items.");
    }

    [Fact]
    public void Validate_MustSeeArtistsCaseInsensitiveDuplicates_Fails()
    {
        var req = Empty with { MustSeeArtists = new[] { "Bob", "bob" } };

        var result = _validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.MustSeeArtists!)
              .WithErrorMessage("MustSeeArtists must not contain duplicates (case-insensitive).");
    }

    [Fact]
    public void Validate_VibeTagItemWhitespacePaddedAtCap_Passes()
    {
        var paddedAtCap = "  " + new string('a', 60) + "  ";
        var req = Empty with { VibeTags = new[] { paddedAtCap } };

        var result = _validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MustSeeArtistsItemWhitespacePaddedAtCap_Passes()
    {
        var paddedAtCap = "  " + new string('a', 200) + "  ";
        var req = Empty with { MustSeeArtists = new[] { paddedAtCap } };

        var result = _validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
