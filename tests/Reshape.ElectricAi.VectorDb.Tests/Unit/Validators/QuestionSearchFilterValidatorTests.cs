using FluentValidation.TestHelper;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.VectorDb.Validators;

namespace Reshape.ElectricAi.VectorDb.Tests.Unit.Validators;

public sealed class QuestionSearchFilterValidatorTests
{
    private readonly QuestionSearchFilterValidator _sut = new();

    [Fact]
    public void Valid_filter_passes()
    {
        var filter = new QuestionSearchFilter("parking");
        _sut.TestValidate(filter).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_filter_with_userContext_passes()
    {
        var filter = new QuestionSearchFilter(
            "parking",
            new Dictionary<Category, IReadOnlyList<string>>
            {
                { Category.Transport, ["Car"] }
            },
            TopK: 10);
        _sut.TestValidate(filter).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_queryText_fails()
    {
        var filter = new QuestionSearchFilter("");
        _sut.TestValidate(filter).ShouldHaveValidationErrorFor(f => f.QueryText);
    }

    [Fact]
    public void QueryText_over_2000_chars_fails()
    {
        var filter = new QuestionSearchFilter(new string('q', 2001));
        _sut.TestValidate(filter).ShouldHaveValidationErrorFor(f => f.QueryText);
    }

    [Fact]
    public void TopK_zero_fails()
    {
        var filter = new QuestionSearchFilter("parking", TopK: 0);
        _sut.TestValidate(filter).ShouldHaveValidationErrorFor(f => f.TopK);
    }

    [Fact]
    public void TopK_51_fails()
    {
        var filter = new QuestionSearchFilter("parking", TopK: 51);
        _sut.TestValidate(filter).ShouldHaveValidationErrorFor(f => f.TopK);
    }

    [Fact]
    public void TopK_1_passes()
    {
        var filter = new QuestionSearchFilter("parking", TopK: 1);
        _sut.TestValidate(filter).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void TopK_50_passes()
    {
        var filter = new QuestionSearchFilter("parking", TopK: 50);
        _sut.TestValidate(filter).ShouldNotHaveAnyValidationErrors();
    }
}
