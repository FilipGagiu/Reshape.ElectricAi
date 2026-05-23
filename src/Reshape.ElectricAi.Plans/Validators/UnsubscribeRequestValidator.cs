using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Notifications;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class UnsubscribeRequestValidator : AbstractValidator<UnsubscribeRequest>
{
    public UnsubscribeRequestValidator()
    {
        RuleFor(x => x.Endpoint)
            .NotEmpty().WithMessage("Endpoint is required.");
    }
}
