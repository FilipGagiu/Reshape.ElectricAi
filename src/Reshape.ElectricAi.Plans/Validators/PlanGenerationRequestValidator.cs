using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Plans;

namespace Reshape.ElectricAi.Plans.Validators;

public sealed class PlanGenerationRequestValidator : AbstractValidator<PlanGenerationRequest>
{
    public PlanGenerationRequestValidator()
    {
        RuleFor(r => r.Answers)
            .NotNull()
            .Must(a => a is { Count: >= 1 and <= 10 })
            .WithMessage("Provide between 1 and 10 answers.");

        RuleForEach(r => r.Answers).ChildRules(a =>
        {
            a.RuleFor(x => x.QuestionId)
                .NotEmpty()
                .MaximumLength(64)
                .Matches("^[a-z][a-z0-9_-]*$")
                .WithMessage("QuestionId must match ^[a-z][a-z0-9_-]*$.");
            a.RuleFor(x => x.QuestionText)
                .NotEmpty()
                .MaximumLength(500);
            a.RuleFor(x => x.Answer)
                .NotEmpty()
                .MaximumLength(2000);
        });

        RuleFor(r => r.FreeText)
            .MaximumLength(2000);
    }
}
