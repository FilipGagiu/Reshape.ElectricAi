namespace Reshape.ElectricAi.Core.Dtos.Plans;

public sealed record PlanGenerationRequest(
    IReadOnlyList<WizardAnswer> Answers,
    string? FreeText);
