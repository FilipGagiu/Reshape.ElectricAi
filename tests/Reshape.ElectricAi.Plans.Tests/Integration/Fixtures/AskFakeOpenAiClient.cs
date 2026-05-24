using System.Text.Json.Nodes;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

internal sealed class AskFakeOpenAiClient : IOpenAiClient
{
    public const string FakeAnswer = "Test answer from AI.";

    public Task<LlmStructuredResult<T>> CompleteStructuredAsync<T>(
        string systemPrompt,
        string userPrompt,
        JsonNode responseSchema,
        string? model,
        int? maxCompletionTokens,
        double? temperature,
        CancellationToken cancellationToken)
        where T : class
        => throw new NotImplementedException("Not used in ask tests.");

    public Task<LlmTextResult> CompleteFreeTextAsync(
        string systemPrompt,
        string userPrompt,
        string? model,
        int? maxCompletionTokens,
        CancellationToken cancellationToken)
        => Task.FromResult(new LlmTextResult(FakeAnswer, new LlmUsage(10, 5, 1)));

    public Task<LlmTextResult> CompleteChatAsync(
        IReadOnlyList<LlmChatMessage> messages,
        string? model,
        int? maxCompletionTokens,
        CancellationToken cancellationToken)
        => Task.FromResult(new LlmTextResult(FakeAnswer, new LlmUsage(10, 5, 1)));
}
