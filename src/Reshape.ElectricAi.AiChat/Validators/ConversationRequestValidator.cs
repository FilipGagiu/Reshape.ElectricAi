using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Conversation;

namespace Reshape.ElectricAi.AiChat.Validators;

public sealed class ConversationRequestValidator : AbstractValidator<ConversationRequest>
{
    public ConversationRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(500);
    }
}
