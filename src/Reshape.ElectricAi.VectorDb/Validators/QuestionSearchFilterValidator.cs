using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;

namespace Reshape.ElectricAi.VectorDb.Validators;

public sealed class QuestionSearchFilterValidator : AbstractValidator<QuestionSearchFilter>
{
    public QuestionSearchFilterValidator()
    {
        RuleFor(f => f.QueryText).NotEmpty().MaximumLength(2000);
        RuleFor(f => f.TopK).InclusiveBetween(1, 50);
    }
}
