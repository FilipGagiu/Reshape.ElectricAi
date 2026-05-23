namespace Reshape.ElectricAi.Core.Services;

public sealed record LlmStructuredResult<T>(T Value, LlmUsage Usage) where T : class;
