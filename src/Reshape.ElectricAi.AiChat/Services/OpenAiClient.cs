using System.ClientModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Reshape.ElectricAi.AiChat.Configuration;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.AiChat.Services;

public sealed partial class OpenAiClient(
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiClient> logger) : IOpenAiClient
{
    private readonly OpenAiOptions _options = options.Value;

    public async Task<LlmStructuredResult<T>> CompleteStructuredAsync<T>(
        string systemPrompt,
        string userPrompt,
        JsonNode responseSchema,
        string? model,
        int? maxCompletionTokens,
        double? temperature,
        CancellationToken cancellationToken)
        where T : class
    {
        var modelId = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;
        if (!_options.Models.TryGetValue(modelId, out var pricing))
        {
            throw new LlmException("model-not-configured", $"No pricing configured for model '{modelId}'.");
        }

        var client = new ChatClient(modelId, new ApiKeyCredential(_options.ApiKey));

        var chatOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: typeof(T).Name,
                jsonSchema: BinaryData.FromString(responseSchema.ToJsonString()),
                jsonSchemaIsStrict: true),
            MaxOutputTokenCount = maxCompletionTokens ?? _options.Limits.MaxCompletionTokens
        };
        if (temperature is not null)
        {
            chatOptions.Temperature = (float)temperature.Value;
        }

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(systemPrompt),
            ChatMessage.CreateUserMessage(userPrompt)
        };

        Exception? lastError = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.Limits.TimeoutSeconds));
                var sw = Stopwatch.StartNew();

                var completion = await client.CompleteChatAsync(messages, chatOptions, cts.Token);
                sw.Stop();

                var json = completion.Value.Content[0].Text;
                T? value;
                try
                {
                    value = JsonSerializer.Deserialize<T>(json, LlmJsonOptions.Default);
                }
                catch (JsonException jex)
                {
                    throw new LlmSchemaException($"JSON deserialization failed: {jex.Message}");
                }

                if (value is null)
                {
                    throw new LlmSchemaException("Deserialized envelope was null.");
                }

                var usage = ComputeUsage(completion.Value.Usage, pricing);
                LogCompleted(logger, modelId, usage.PromptTokens, usage.CompletionTokens, usage.CostCents, sw.ElapsedMilliseconds);
                return new LlmStructuredResult<T>(value, usage);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (LlmException)
            {
                // LlmSchemaException + transient LlmException are terminal — retrying the
                // same prompt against the same schema would burn another paid call for
                // identical output. Future retry strategy must augment the prompt with
                // corrective feedback before re-issue.
                throw;
            }
            catch (Exception ex) when (attempt == 1 && IsTransient(ex))
            {
                lastError = ex;
                LogRetry(logger, attempt, ex.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                throw new LlmException("llm-unavailable", "OpenAI call failed.", ex);
            }
        }

        throw new LlmException("llm-unavailable", "OpenAI retries exhausted.", lastError);
    }

    public async Task<LlmTextResult> CompleteFreeTextAsync(
        string systemPrompt,
        string userPrompt,
        string? model,
        int? maxCompletionTokens,
        CancellationToken cancellationToken)
    {
        var modelId = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;
        if (!_options.Models.TryGetValue(modelId, out var pricing))
        {
            throw new LlmException("model-not-configured", $"No pricing configured for model '{modelId}'.");
        }

        var client = new ChatClient(modelId, new ApiKeyCredential(_options.ApiKey));

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = maxCompletionTokens ?? _options.Limits.MaxCompletionTokens
        };

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(systemPrompt),
            ChatMessage.CreateUserMessage(userPrompt)
        };

        Exception? lastError = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.Limits.TimeoutSeconds));
                var sw = Stopwatch.StartNew();

                var completion = await client.CompleteChatAsync(messages, chatOptions, cts.Token);
                sw.Stop();

                var text = completion.Value.Content[0].Text;
                var usage = ComputeUsage(completion.Value.Usage, pricing);
                LogCompleted(logger, modelId, usage.PromptTokens, usage.CompletionTokens, usage.CostCents, sw.ElapsedMilliseconds);
                return new LlmTextResult(text, usage);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (LlmException)
            {
                throw;
            }
            catch (Exception ex) when (attempt == 1 && IsTransient(ex))
            {
                lastError = ex;
                LogRetry(logger, attempt, ex.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                throw new LlmException("llm-unavailable", "OpenAI call failed.", ex);
            }
        }

        throw new LlmException("llm-unavailable", "OpenAI retries exhausted.", lastError);
    }

    private static bool IsTransient(Exception ex) =>
        ex is TaskCanceledException
        || ex is ClientResultException;

    private static LlmUsage ComputeUsage(ChatTokenUsage tokenUsage, OpenAiOptions.ModelPricing pricing)
    {
        var promptTokens = tokenUsage.InputTokenCount;
        var completionTokens = tokenUsage.OutputTokenCount;
        var costCents = (int)Math.Ceiling(
            (promptTokens / 1000m) * pricing.PromptCentsPer1K +
            (completionTokens / 1000m) * pricing.CompletionCentsPer1K);
        return new LlmUsage(promptTokens, completionTokens, costCents);
    }

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information,
        Message = "OpenAI call completed: model={Model}, promptTokens={PromptTokens}, completionTokens={CompletionTokens}, costCents={CostCents}, elapsedMs={ElapsedMs}")]
    private static partial void LogCompleted(ILogger logger, string model, int promptTokens, int completionTokens, int costCents, long elapsedMs);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "OpenAI call retrying (attempt {Attempt}): {Reason}")]
    private static partial void LogRetry(ILogger logger, int attempt, string reason);
}
