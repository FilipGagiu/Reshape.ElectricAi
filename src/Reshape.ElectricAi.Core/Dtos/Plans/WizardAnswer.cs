namespace Reshape.ElectricAi.Core.Dtos.Plans;

public sealed record WizardAnswer(
    string QuestionId,
    string QuestionText,
    string Answer);
