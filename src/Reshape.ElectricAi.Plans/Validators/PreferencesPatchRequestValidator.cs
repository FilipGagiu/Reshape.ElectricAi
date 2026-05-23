using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Preferences;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class PreferencesPatchRequestValidator : AbstractValidator<PreferencesPatchRequest>
{
    public PreferencesPatchRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(80).When(x => x.Name is not null)
            .WithMessage("Name must be 80 characters or fewer.");

        RuleFor(x => x.Origin)
            .MaximumLength(120).When(x => x.Origin is not null)
            .WithMessage("Origin must be 120 characters or fewer.");

        When(x => x.Crew is not null, () =>
        {
            RuleFor(x => x.Crew!.Kind).IsInEnum().WithMessage("Crew.Kind must be a valid value.");
            RuleFor(x => x.Crew!.EstimatedSize)
                .InclusiveBetween(1, 200)
                .When(x => x.Crew!.EstimatedSize is not null)
                .WithMessage("Crew.EstimatedSize must be between 1 and 200.");
        });

        RuleFor(x => x.TicketType)
            .IsInEnum().When(x => x.TicketType is not null)
            .WithMessage("TicketType must be a valid value.");

        RuleFor(x => x.AgeGroup)
            .IsInEnum().When(x => x.AgeGroup is not null)
            .WithMessage("AgeGroup must be a valid value.");

        When(x => x.SuggestedTransport is not null, () =>
        {
            RuleFor(x => x.SuggestedTransport!.Mode)
                .IsInEnum().WithMessage("SuggestedTransport.Mode must be a valid value.");
            RuleFor(x => x.SuggestedTransport!.Note)
                .MaximumLength(200).When(x => x.SuggestedTransport!.Note is not null)
                .WithMessage("SuggestedTransport.Note must be 200 characters or fewer.");
        });

        When(x => x.SuggestedAccommodation is not null, () =>
        {
            RuleFor(x => x.SuggestedAccommodation!.Type)
                .IsInEnum().WithMessage("SuggestedAccommodation.Type must be a valid value.");
            RuleFor(x => x.SuggestedAccommodation!.Note)
                .MaximumLength(200).When(x => x.SuggestedAccommodation!.Note is not null)
                .WithMessage("SuggestedAccommodation.Note must be 200 characters or fewer.");
        });

        When(x => x.VibeTags is not null, () =>
        {
            RuleFor(x => x.VibeTags!.Count)
                .LessThanOrEqualTo(6).WithMessage("VibeTags must contain at most 6 items.");
            RuleForEach(x => x.VibeTags!)
                .NotEmpty().WithMessage("VibeTag values must not be empty.")
                .Must(value => value is not null && value.Trim().Length is >= 1 and <= 60)
                .WithMessage("VibeTag values must be 1 to 60 characters.");
        });

        When(x => x.MusicGenres is not null, () =>
        {
            RuleFor(x => x.MusicGenres!.Count)
                .LessThanOrEqualTo(11).WithMessage("MusicGenres must contain at most 11 items.");
            RuleForEach(x => x.MusicGenres!)
                .IsInEnum().WithMessage("MusicGenres contains an invalid value.");
            RuleFor(x => x.MusicGenres!)
                .Must(items => items.Distinct().Count() == items.Count)
                .WithMessage("MusicGenres must not contain duplicates.");
        });

        When(x => x.FoodRestrictions is not null, () =>
        {
            RuleFor(x => x.FoodRestrictions!.Count)
                .LessThanOrEqualTo(11).WithMessage("FoodRestrictions must contain at most 11 items.");
            RuleForEach(x => x.FoodRestrictions!)
                .IsInEnum().WithMessage("FoodRestrictions contains an invalid value.");
            RuleFor(x => x.FoodRestrictions!)
                .Must(items => items.Distinct().Count() == items.Count)
                .WithMessage("FoodRestrictions must not contain duplicates.");
        });

        When(x => x.Cuisines is not null, () =>
        {
            RuleFor(x => x.Cuisines!.Count)
                .LessThanOrEqualTo(15).WithMessage("Cuisines must contain at most 15 items.");
            RuleForEach(x => x.Cuisines!)
                .IsInEnum().WithMessage("Cuisines contains an invalid value.");
            RuleFor(x => x.Cuisines!)
                .Must(items => items.Distinct().Count() == items.Count)
                .WithMessage("Cuisines must not contain duplicates.");
        });

        When(x => x.ActivityInterests is not null, () =>
        {
            RuleFor(x => x.ActivityInterests!.Count)
                .LessThanOrEqualTo(7).WithMessage("ActivityInterests must contain at most 7 items.");
            RuleForEach(x => x.ActivityInterests!)
                .IsInEnum().WithMessage("ActivityInterests contains an invalid value.");
            RuleFor(x => x.ActivityInterests!)
                .Must(items => items.Distinct().Count() == items.Count)
                .WithMessage("ActivityInterests must not contain duplicates.");
        });

        When(x => x.MustSeeArtists is not null, () =>
        {
            RuleFor(x => x.MustSeeArtists!.Count)
                .LessThanOrEqualTo(20).WithMessage("MustSeeArtists must contain at most 20 items.");
            RuleForEach(x => x.MustSeeArtists!)
                .NotEmpty().WithMessage("MustSeeArtists names must not be empty.")
                .Must(name => name is not null && name.Trim().Length is >= 1 and <= 200)
                .WithMessage("MustSeeArtists names must be 1 to 200 characters.");
            RuleFor(x => x.MustSeeArtists!)
                .Must(items => items
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim().ToLowerInvariant())
                    .Distinct()
                    .Count() == items.Count(s => !string.IsNullOrWhiteSpace(s)))
                .WithMessage("MustSeeArtists must not contain duplicates (case-insensitive).");
        });
    }
}
