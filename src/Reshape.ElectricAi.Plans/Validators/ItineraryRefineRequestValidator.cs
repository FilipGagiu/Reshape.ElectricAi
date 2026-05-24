using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Itinerary;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class ItineraryRefineRequestValidator : AbstractValidator<ItineraryRefineRequest>
{
    public ItineraryRefineRequestValidator()
    {
        RuleFor(x => x.ItineraryId).NotEmpty().WithMessage("itinerary-id-required");
        RuleFor(x => x.FreeText)
            .NotEmpty().WithMessage("free-text-required")
            .MaximumLength(2000).WithMessage("free-text-too-long");
        RuleFor(x => x.Locale)
            .MaximumLength(8).WithMessage("locale-too-long")
            .Matches("^[a-zA-Z]{2,3}(-[a-zA-Z]{2,4})?$").WithMessage("locale-invalid")
            .When(x => x.Locale is not null);
    }
}
