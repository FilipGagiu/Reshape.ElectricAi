using FluentValidation.TestHelper;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.VectorDb.Validators;

namespace Reshape.ElectricAi.VectorDb.Tests.Unit.Validators;

public sealed class IngestQARequestValidatorTests
{
    private readonly IngestQARequestValidator _sut = new();

    [Fact]
    public void Valid_request_passes()
    {
        var req = new IngestQARequest("Where is the medical tent?", [new IngestAnswerRequest("Near the East entrance.")]);
        _sut.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_questionText_fails_on_QuestionText()
    {
        var req = new IngestQARequest("", [new IngestAnswerRequest("Answer.")]);
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(r => r.QuestionText);
    }

    [Fact]
    public void QuestionText_over_2000_chars_fails()
    {
        var req = new IngestQARequest(new string('x', 2001), [new IngestAnswerRequest("Answer.")]);
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(r => r.QuestionText);
    }

    [Fact]
    public void Empty_answers_list_fails()
    {
        var req = new IngestQARequest("Question?", []);
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(r => r.Answers);
    }

    [Fact]
    public void Empty_answer_text_fails()
    {
        var req = new IngestQARequest("Question?", [new IngestAnswerRequest("")]);
        _sut.TestValidate(req).ShouldHaveValidationErrorFor("Answers[0].AnswerText");
    }

    [Fact]
    public void AnswerText_over_4000_chars_fails()
    {
        var req = new IngestQARequest("Question?", [new IngestAnswerRequest(new string('y', 4001))]);
        _sut.TestValidate(req).ShouldHaveValidationErrorFor("Answers[0].AnswerText");
    }
}
