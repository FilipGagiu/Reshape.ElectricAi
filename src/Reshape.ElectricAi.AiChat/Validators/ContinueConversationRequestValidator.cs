using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Conversation;

namespace Reshape.ElectricAi.AiChat.Validators;

public sealed class ContinueConversationRequestValidator : AbstractValidator<ContinueConversationRequest>
{
    public ContinueConversationRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
    }
}
