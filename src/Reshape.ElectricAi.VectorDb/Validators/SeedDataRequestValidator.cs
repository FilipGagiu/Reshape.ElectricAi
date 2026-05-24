using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;

namespace Reshape.ElectricAi.VectorDb.Validators;

public sealed class SeedDataRequestValidator : AbstractValidator<SeedDataRequest>
{
    public SeedDataRequestValidator()
    {
        RuleFor(x => x.DataPath)
            .NotEmpty()
            .WithMessage("DataPath is required.");
    }
}
