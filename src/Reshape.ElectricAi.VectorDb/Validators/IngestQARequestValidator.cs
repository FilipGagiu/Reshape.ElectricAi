using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;

namespace Reshape.ElectricAi.VectorDb.Validators;

public sealed class IngestQARequestValidator : AbstractValidator<IngestQARequest>
{
    public IngestQARequestValidator()
    {
        RuleFor(r => r.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(r => r.Answers).NotEmpty().WithMessage("At least one answer is required.");
        RuleForEach(r => r.Answers).ChildRules(a =>
            a.RuleFor(x => x.AnswerText).NotEmpty().MaximumLength(4000));
    }
}
