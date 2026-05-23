using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Groups;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class AddGroupMemberRequestValidator : AbstractValidator<AddGroupMemberRequest>
{
    public AddGroupMemberRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid address.")
            .MaximumLength(256).WithMessage("Email must be 256 characters or fewer.");
    }
}
