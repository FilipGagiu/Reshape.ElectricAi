using FluentValidation;
using Reshape.ElectricAi.LiveFeed.Dtos;

namespace Reshape.ElectricAi.LiveFeed.Validators;

public sealed class PublishFeedEntryRequestValidator : AbstractValidator<PublishFeedEntryRequest>
{
    public PublishFeedEntryRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Body).NotEmpty().MaximumLength(4000);
        RuleFor(r => r.PrimaryCategory).IsInEnum();

        RuleFor(r => r.TargetArtists)
            .NotNull()
            .Must(list => list.Count <= 25).WithMessage("Too many target artists (max 25)")
            .Must(list => list.All(a => !string.IsNullOrWhiteSpace(a) && a.Length <= 100))
                .WithMessage("Each artist 1..100 chars")
            .Must(list => list.Distinct(StringComparer.OrdinalIgnoreCase).Count() == list.Count)
                .WithMessage("Duplicate artist names");

        RuleFor(r => r.TargetGenres)
            .NotNull()
            .Must(list => list.Count <= 12).WithMessage("Too many target genres (max 12)")
            .Must(list => list.All(g => Enum.IsDefined(g))).WithMessage("Unknown genre value")
            .Must(list => list.Distinct().Count() == list.Count).WithMessage("Duplicate genres");

        RuleFor(r => r)
            .Must(r => r.IsGeneral || r.TargetArtists.Count > 0 || r.TargetGenres.Count > 0)
            .WithErrorCode("no-targeting-and-not-general")
            .WithMessage("Entry must be general or target at least one artist/genre");
    }
}
