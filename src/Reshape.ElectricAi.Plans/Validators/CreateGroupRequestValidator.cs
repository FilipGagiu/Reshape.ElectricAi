using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Groups;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Group name must not be whitespace.")
            .MaximumLength(100).WithMessage("Group name must be 100 characters or fewer.");
    }
}
