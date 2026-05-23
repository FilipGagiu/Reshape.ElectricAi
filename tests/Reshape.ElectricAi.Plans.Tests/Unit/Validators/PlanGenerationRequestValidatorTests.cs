using FluentAssertions;
using FluentValidation.TestHelper;
using Reshape.ElectricAi.Core.Dtos.Plans;
using Reshape.ElectricAi.Plans.Validators;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Validators;

public sealed class PlanGenerationRequestValidatorTests
{
    private readonly PlanGenerationRequestValidator _sut = new();

    [Fact]
    public void Validate_EmptyAnswers_HasError()
    {
        var req = new PlanGenerationRequest(Answers: Array.Empty<WizardAnswer>(), FreeText: null);
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(r => r.Answers);
    }

    [Fact]
    public void Validate_TooManyAnswers_HasError()
    {
        var answers = Enumerable.Range(0, 11)
            .Select(i => new WizardAnswer($"q-{i}", "Q", "A"))
            .ToList();
        var req = new PlanGenerationRequest(answers, FreeText: null);
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(r => r.Answers);
    }

    [Fact]
    public void Validate_FreeTextTooLong_HasError()
    {
        var req = new PlanGenerationRequest(
            new[] { new WizardAnswer("q", "Q", "A") },
            FreeText: new string('x', 2001));
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(r => r.FreeText);
    }

    [Fact]
    public void Validate_AnswerTooLong_HasError()
    {
        var req = new PlanGenerationRequest(
            new[] { new WizardAnswer("q", "Q", new string('a', 2001)) },
            FreeText: null);
        var result = _sut.TestValidate(req);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_QuestionIdInvalidPattern_HasError()
    {
        var req = new PlanGenerationRequest(
            new[] { new WizardAnswer("Q!Id", "Q", "A") },
            FreeText: null);
        var result = _sut.TestValidate(req);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_HappyPath_NoErrors()
    {
        var req = new PlanGenerationRequest(
            new[]
            {
                new WizardAnswer("vibe", "What's your vibe?", "chill"),
                new WizardAnswer("budget", "Budget?", "around 800 RON")
            },
            FreeText: "bringing my dog");
        _sut.TestValidate(req).IsValid.Should().BeTrue();
    }
}
