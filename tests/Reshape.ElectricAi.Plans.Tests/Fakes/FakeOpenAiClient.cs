using System.Text.Json;
using System.Text.Json.Nodes;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Plans.Tests.Fakes;

public sealed class FakeOpenAiClient : IOpenAiClient
{
    private readonly Queue<QueuedResponse> _responses = new();
    private readonly List<RecordedCall> _calls = new();

    public int CallCount => _calls.Count;
    public IReadOnlyList<RecordedCall> Calls => _calls;

    public FakeOpenAiClient WithEnvelope(object envelope, LlmUsage? usage = null)
    {
        var json = JsonSerializer.Serialize(envelope, LlmJsonOptions.Default);
        var resolvedUsage = usage ?? new LlmUsage(10, 5, 1);
        _responses.Enqueue(new QueuedResponse(json, null, null, resolvedUsage));
        return this;
    }

    public FakeOpenAiClient WithTextResponse(string text, LlmUsage? usage = null)
    {
        var resolvedUsage = usage ?? new LlmUsage(10, 5, 1);
        _responses.Enqueue(new QueuedResponse(null, text, null, resolvedUsage));
        return this;
    }

    public FakeOpenAiClient WithException(Exception ex)
    {
        _responses.Enqueue(new QueuedResponse(null, null, ex, new LlmUsage(0, 0, 0)));
        return this;
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
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Add(new RecordedCall(systemPrompt, userPrompt, model, maxCompletionTokens, temperature));
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("FakeOpenAiClient has no queued response.");
        }

        var queued = _responses.Dequeue();
        if (queued.Exception is not null)
        {
            throw queued.Exception;
        }

        var value = JsonSerializer.Deserialize<T>(queued.Json!, LlmJsonOptions.Default)
            ?? throw new InvalidOperationException("FakeOpenAiClient queued envelope deserialized to null.");
        return Task.FromResult(new LlmStructuredResult<T>(value, queued.Usage));
    }

    public Task<LlmTextResult> CompleteFreeTextAsync(
        string systemPrompt,
        string userPrompt,
        string? model,
        int? maxCompletionTokens,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Add(new RecordedCall(systemPrompt, userPrompt, model, maxCompletionTokens, null));
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("FakeOpenAiClient has no queued response.");
        }

        var queued = _responses.Dequeue();
        if (queued.Exception is not null)
        {
            throw queued.Exception;
        }

        return Task.FromResult(new LlmTextResult(queued.Text ?? string.Empty, queued.Usage));
    }

    private sealed record QueuedResponse(string? Json, string? Text, Exception? Exception, LlmUsage Usage);
}

public sealed record RecordedCall(
    string SystemPrompt,
    string UserPrompt,
    string? Model,
    int? MaxCompletionTokens,
    double? Temperature);
