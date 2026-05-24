using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Ask;

namespace Reshape.ElectricAi.AiChat.Validators;

public sealed class AskRequestValidator : AbstractValidator<AskRequest>
{
    public AskRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(500);
    }
}
