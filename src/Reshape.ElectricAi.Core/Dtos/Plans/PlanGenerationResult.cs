using Reshape.ElectricAi.Core.Dtos.Preferences;

namespace Reshape.ElectricAi.Core.Dtos.Plans;

public sealed record PlanGenerationResult(
    PlanDto Plan,
    PreferencesDto Preferences,
    string Tip);
