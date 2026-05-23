using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Groups;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class GroupPreferencesReplaceRequestValidator : AbstractValidator<GroupPreferencesReplaceRequest>
{
    public GroupPreferencesReplaceRequestValidator()
    {
        RuleFor(x => x.TicketType)
            .IsInEnum().When(x => x.TicketType is not null)
            .WithMessage("TicketType must be a valid value.");

        RuleFor(x => x.Accommodation)
            .IsInEnum().When(x => x.Accommodation is not null)
            .WithMessage("Accommodation must be a valid value.");

        RuleFor(x => x.Transport)
            .IsInEnum().When(x => x.Transport is not null)
            .WithMessage("Transport must be a valid value.");

        RuleFor(x => x.AgeGroup)
            .IsInEnum().When(x => x.AgeGroup is not null)
            .WithMessage("AgeGroup must be a valid value.");

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

        When(x => x.Activities is not null, () =>
        {
            RuleFor(x => x.Activities!.Count)
                .LessThanOrEqualTo(7).WithMessage("Activities must contain at most 7 items.");
            RuleForEach(x => x.Activities!)
                .IsInEnum().WithMessage("Activities contains an invalid value.");
            RuleFor(x => x.Activities!)
                .Must(items => items.Distinct().Count() == items.Count)
                .WithMessage("Activities must not contain duplicates.");
        });

        When(x => x.Artists is not null, () =>
        {
            RuleFor(x => x.Artists!.Count)
                .LessThanOrEqualTo(20).WithMessage("Artists must contain at most 20 items.");
            RuleForEach(x => x.Artists!)
                .NotEmpty().WithMessage("Artist names must not be empty.")
                .Must(name => name is not null && name.Trim().Length is >= 1 and <= 200)
                .WithMessage("Artist names must be 1 to 200 characters.");
            RuleFor(x => x.Artists!)
                .Must(items => items
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim().ToLowerInvariant())
                    .Distinct()
                    .Count() == items.Count(s => !string.IsNullOrWhiteSpace(s)))
                .WithMessage("Artists must not contain duplicates (case-insensitive).");
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
    }
}
