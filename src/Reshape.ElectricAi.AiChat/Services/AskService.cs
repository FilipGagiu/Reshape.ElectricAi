using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.AiChat.Configuration;
using Reshape.ElectricAi.Core.Dtos.Ask;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.AiChat.Services;

internal sealed partial class AskService(
    IVectorSearchService vectorSearch,
    IOpenAiClient openAi,
    IOptions<AskOptions> options,
    ILogger<AskService> logger) : IAskService
{
    private readonly AskOptions _options = options.Value;

    public async Task<AskResponse> AskAsync(
        AskRequest request,
        CancellationToken cancellationToken = default)
    {
        var topK = _options.TopKPerSource;

        // Sequential: all three searches share the same scoped VectorDbContext instance,
        // which does not support concurrent operations on the same connection.
        // UserContext is forwarded identically into each filter so a caller's category
        // map narrows retrieval the same way across documents, FAQ, and events
        // (mirrors the POST /api/v1/faq/search semantics).
        var chunks = await vectorSearch.SearchDocumentsAsync(
            new DocumentSearchFilter(request.QuestionText, request.UserContext, TopK: topK), cancellationToken);
        var qas = await vectorSearch.SearchQuestionsAsync(
            new QuestionSearchFilter(request.QuestionText, request.UserContext, TopK: topK), cancellationToken);
        var events = await vectorSearch.SearchEventsAsync(
            new EventSearchFilter(request.QuestionText, request.UserContext, TopK: topK), cancellationToken);

        var merged = chunks.Select(c => new ScoredContext(c.Content, c.Score))
            .Concat(qas.Select(q => new ScoredContext(BuildQaText(q), q.QuestionScore)))
            .Concat(events.Select(e => new ScoredContext(e.TextRepresentation, e.Score)))
            .OrderByDescending(x => x.Score)
            .Where(x => x.Score > _options.ScoreThreshold)
            .Take(_options.TopKFinal)
            .ToList();

        LogRetrieved(logger, merged.Count, _options.ScoreThreshold);

        if (merged.Count == 0)
        {
            return new AskResponse(ChatPrompts.FallbackAnswer);
        }

        var userPrompt = $"{BuildContextBlock(merged)}\n\nQuestion: {request.QuestionText}";

        var result = await openAi.CompleteFreeTextAsync(
            ChatPrompts.SystemPrompt,
            userPrompt,
            _options.Model,
            _options.MaxCompletionTokens,
            cancellationToken);

        LogAnswered(logger, result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.CostCents);

        return new AskResponse(result.Text);
    }

    private static string BuildQaText(RetrievedQA qa)
    {
        if (qa.Answers.Count == 0)
            return $"Q: {qa.QuestionText}";
        var answers = string.Join(" | ", qa.Answers.Select(a => a.AnswerText));
        return $"Q: {qa.QuestionText}\nA: {answers}";
    }

    private static string BuildContextBlock(List<ScoredContext> items)
    {
        var sb = new StringBuilder("Context:");
        for (var i = 0; i < items.Count; i++)
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n[{i + 1}] {items[i].Text}");
        }
        return sb.ToString();
    }

    [LoggerMessage(EventId = 7001, Level = LogLevel.Information,
        Message = "Ask retrieval: hitCount={HitCount}, threshold={Threshold}")]
    private static partial void LogRetrieved(ILogger logger, int hitCount, float threshold);

    [LoggerMessage(EventId = 7002, Level = LogLevel.Information,
        Message = "Ask answered: promptTokens={PromptTokens}, completionTokens={CompletionTokens}, costCents={CostCents}")]
    private static partial void LogAnswered(ILogger logger, int promptTokens, int completionTokens, int costCents);

    private sealed record ScoredContext(string Text, float Score);
}
