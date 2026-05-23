using FluentValidation;
using Reshape.ElectricAi.LiveFeed.Dtos;

namespace Reshape.ElectricAi.LiveFeed.Validators;

public sealed class UpdateFeedEntryRequestValidator : AbstractValidator<UpdateFeedEntryRequest>
{
    public UpdateFeedEntryRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Body).NotEmpty().MaximumLength(4000);
        RuleFor(r => r.PrimaryCategory).IsInEnum();

        RuleFor(r => r.TargetArtists)
            .NotNull()
            .Must(list => list.Count <= 25)
            .Must(list => list.All(a => !string.IsNullOrWhiteSpace(a) && a.Length <= 100))
            .Must(list => list.Distinct(StringComparer.OrdinalIgnoreCase).Count() == list.Count);

        RuleFor(r => r.TargetGenres)
            .NotNull()
            .Must(list => list.Count <= 12)
            .Must(list => list.All(g => Enum.IsDefined(g)))
            .Must(list => list.Distinct().Count() == list.Count);

        RuleFor(r => r)
            .Must(r => r.IsGeneral || r.TargetArtists.Count > 0 || r.TargetGenres.Count > 0)
            .WithErrorCode("no-targeting-and-not-general")
            .WithMessage("Entry must be general or target at least one artist/genre");
    }
}
