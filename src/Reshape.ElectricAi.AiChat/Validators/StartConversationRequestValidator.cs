using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Conversation;

namespace Reshape.ElectricAi.AiChat.Validators;

public sealed class StartConversationRequestValidator : AbstractValidator<StartConversationRequest>
{
    public StartConversationRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
    }
}
