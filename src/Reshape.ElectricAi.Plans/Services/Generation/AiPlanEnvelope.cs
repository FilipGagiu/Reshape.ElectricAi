namespace Reshape.ElectricAi.Plans.Services.Generation;

internal sealed record AiPlanEnvelope(
    AiPreferences Preferences,
    AiPlan Plan,
    string Tip);
