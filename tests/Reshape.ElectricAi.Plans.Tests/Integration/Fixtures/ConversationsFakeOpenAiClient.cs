using System.Text.Json.Nodes;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

public sealed class ConversationsFakeOpenAiClient : IOpenAiClient
{
    private readonly List<IReadOnlyList<LlmChatMessage>> _captured = new();
    private readonly object _gate = new();

    public bool ThrowOnNextCall { get; set; }

    public IReadOnlyList<IReadOnlyList<LlmChatMessage>> Captured
    {
        get
        {
            lock (_gate)
            {
                return _captured.ToArray();
            }
        }
    }

    public Task<LlmStructuredResult<T>> CompleteStructuredAsync<T>(
        string systemPrompt,
        string userPrompt,
        JsonNode responseSchema,
        string? model,
        int? maxCompletionTokens,
        double? temperature,
        CancellationToken cancellationToken)
        where T : class
        => throw new NotImplementedException("Not used in conversation tests.");

    public Task<LlmTextResult> CompleteFreeTextAsync(
        string systemPrompt,
        string userPrompt,
        string? model,
        int? maxCompletionTokens,
        CancellationToken cancellationToken)
        => Task.FromResult(new LlmTextResult("free-text-fallback", new LlmUsage(10, 5, 1)));

    public Task<LlmTextResult> CompleteChatAsync(
        IReadOnlyList<LlmChatMessage> messages,
        string? model,
        int? maxCompletionTokens,
        CancellationToken cancellationToken)
    {
        if (ThrowOnNextCall)
        {
            ThrowOnNextCall = false;
            throw new InvalidOperationException("Forced failure for test.");
        }

        lock (_gate)
        {
            _captured.Add(messages);
        }

        // Echo the last user turn so tests can assert prompt-flow correctness.
        var lastUser = messages.LastOrDefault(m => m.Role == LlmChatRole.User);
        var reply = lastUser is null
            ? "no user message"
            : $"echo: {Excerpt(lastUser.Content)}";
        return Task.FromResult(new LlmTextResult(reply, new LlmUsage(10, 5, 1)));
    }

    private static string Excerpt(string s) =>
        s.Length <= 80 ? s : string.Concat(s.AsSpan(0, 77), "...");
}
