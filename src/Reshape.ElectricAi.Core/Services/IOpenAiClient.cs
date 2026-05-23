using System.Text.Json.Nodes;

namespace Reshape.ElectricAi.Core.Services;

public interface IOpenAiClient
{
    Task<LlmStructuredResult<T>> CompleteStructuredAsync<T>(
        string systemPrompt,
        string userPrompt,
        JsonNode responseSchema,
        string? model,
        int? maxCompletionTokens,
        double? temperature,
        CancellationToken cancellationToken)
        where T : class;
}
