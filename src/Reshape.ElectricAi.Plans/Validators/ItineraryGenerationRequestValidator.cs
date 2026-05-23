using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Itinerary;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class ItineraryGenerationRequestValidator : AbstractValidator<ItineraryGenerationRequest>
{
    public ItineraryGenerationRequestValidator()
    {
        RuleFor(x => x.Version).Equal(1).WithMessage("unsupported-version");
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(8);

        RuleFor(x => x)
            .Must(r => r.Answers is { Count: > 0 } || !string.IsNullOrWhiteSpace(r.FreeText))
            .WithMessage("preferences-required");

        When(x => x.Answers is not null, () =>
        {
            RuleFor(x => x.Answers.Count)
                .LessThanOrEqualTo(20).WithMessage("Answers must contain at most 20 items.");
            RuleForEach(x => x.Answers).ChildRules(a =>
            {
                a.RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
                a.RuleFor(x => x.Answer).NotEmpty().MaximumLength(2000);
            });
        });

        RuleFor(x => x.FreeText)
            .MaximumLength(4000)
            .When(x => x.FreeText is not null);
    }
}
