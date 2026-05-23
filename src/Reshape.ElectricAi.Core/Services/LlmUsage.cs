namespace Reshape.ElectricAi.Core.Services;

public sealed record LlmUsage(
    int PromptTokens,
    int CompletionTokens,
    int CostCents);
