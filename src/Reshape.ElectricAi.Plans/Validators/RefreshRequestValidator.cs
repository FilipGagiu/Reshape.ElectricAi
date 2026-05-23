using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Auth;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
