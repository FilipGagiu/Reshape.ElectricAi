using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.AiChat.Configuration;
using Reshape.ElectricAi.AiChat.Entities;
using Reshape.ElectricAi.AiChat.Persistence;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Conversation;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.AiChat.Services;

internal sealed partial class ConversationService(
    ChatDbContext db,
    IVectorSearchService vectorSearch,
    IOpenAiClient openAi,
    IOptions<ConversationOptions> options,
    ILogger<ConversationService> logger) : IConversationService
{
    private readonly ConversationOptions _options = options.Value;

    public async Task<IReadOnlyList<ConversationSummaryDto>> ListAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await db.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageUtc)
            .Select(c => new ConversationSummaryDto(
                c.Id, c.Title, c.CreatedUtc, c.LastMessageUtc, c.UserMessageCount))
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<ConversationDetailDto> GetAsync(
        Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conv = await db.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId && c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.CreatedUtc,
                Messages = c.Messages
                    .OrderBy(m => m.OrderIndex)
                    .Select(m => new ReplyDto(m.Content, m.Actor, m.CreatedUtc))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (conv is null)
        {
            throw new NotFoundException("not-found", "Conversation not found.");
        }

        return new ConversationDetailDto(conv.Id, conv.Title, conv.CreatedUtc, conv.Messages);
    }

    public async Task<StartConversationResponse> StartAsync(
        Guid userId, StartConversationRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var conv = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = ConversationTitleHelper.Derive(request.Message, _options.TitleMaxChars),
            CreatedUtc = now,
            LastMessageUtc = now,
            UserMessageCount = 0,
            IsGenerating = true,
            GeneratingStartedUtc = now
        };
        db.Conversations.Add(conv);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var reply = await RunTurnAsync(conv, request.Message, request.UserContext, cancellationToken);
            return new StartConversationResponse(conv.Id, conv.Title, reply);
        }
        finally
        {
            await ReleaseLockAsync(conv.Id);
        }
    }

    public async Task<ContinueConversationResponse> ContinueAsync(
        Guid userId, Guid conversationId, ContinueConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleCutoff = now.AddSeconds(-_options.LockTimeoutSeconds);

        var acquired = await db.Conversations
            .Where(c => c.Id == conversationId
                        && c.UserId == userId
                        && c.UserMessageCount < _options.UserMessageCap
                        && (!c.IsGenerating || (c.GeneratingStartedUtc != null && c.GeneratingStartedUtc < staleCutoff)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsGenerating, true)
                .SetProperty(c => c.GeneratingStartedUtc, (DateTime?)now),
                cancellationToken);

        if (acquired == 0)
        {
            await DiagnoseAcquireFailureAsync(userId, conversationId, staleCutoff, cancellationToken);
        }

        // Reload as tracked so subsequent navigation Add + SaveChanges work cleanly.
        var conv = await db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.OrderIndex))
            .FirstAsync(c => c.Id == conversationId, cancellationToken);

        try
        {
            var reply = await RunTurnAsync(conv, request.Message, request.UserContext, cancellationToken);
            return new ContinueConversationResponse(reply);
        }
        finally
        {
            await ReleaseLockAsync(conv.Id);
        }
    }

    /// <summary>
    /// Append the user message, run RAG + LLM, append the bot reply, and update counters.
    /// Lock is assumed already held by the caller; releasing it is the caller's responsibility.
    /// </summary>
    private async Task<ReplyDto> RunTurnAsync(
        Conversation conv,
        string userMessage,
        IReadOnlyDictionary<Category, IReadOnlyList<string>>? userContext,
        CancellationToken cancellationToken)
    {
        var baseOrder = conv.Messages.Count;
        var userMsgTime = DateTime.UtcNow;

        var userEntity = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conv.Id,
            Actor = ConversationActor.User,
            Content = userMessage,
            CreatedUtc = userMsgTime,
            OrderIndex = baseOrder
        };
        // Use the DbSet explicitly so EF marks the entity Added (rather than Modified, which
        // happens when adding to a tracked nav collection with a pre-set Guid Id).
        // Auto-fixup will still populate `conv.Messages` so BuildLlmMessages sees it below.
        db.ConversationMessages.Add(userEntity);

        // Sequential RAG: scoped VectorDbContext does not support concurrent queries.
        var topK = _options.TopKPerSource;
        var chunks = await vectorSearch.SearchDocumentsAsync(
            new DocumentSearchFilter(userMessage, userContext, TopK: topK), cancellationToken);
        var qas = await vectorSearch.SearchQuestionsAsync(
            new QuestionSearchFilter(userMessage, userContext, TopK: topK), cancellationToken);
        var events = await vectorSearch.SearchEventsAsync(
            new EventSearchFilter(userMessage, userContext, TopK: topK), cancellationToken);

        var merged = chunks.Select(c => new ScoredContext(c.Content, c.Score))
            .Concat(qas.Select(q => new ScoredContext(BuildQaText(q), q.QuestionScore)))
            .Concat(events.Select(e => new ScoredContext(e.TextRepresentation, e.Score)))
            .OrderByDescending(x => x.Score)
            .Where(x => x.Score > _options.ScoreThreshold)
            .Take(_options.TopKFinal)
            .ToList();

        LogRetrieved(logger, conv.Id, merged.Count, _options.ScoreThreshold);

        var orderedHistory = conv.Messages.OrderBy(m => m.OrderIndex).ToList();
        var llmMessages = BuildLlmMessages(orderedHistory, merged, userMessage);

        var result = await openAi.CompleteChatAsync(
            llmMessages,
            _options.Model,
            _options.MaxCompletionTokens,
            cancellationToken);

        LogAnswered(logger, conv.Id, result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.CostCents);

        var botMsgTime = DateTime.UtcNow;
        var botContent = string.IsNullOrWhiteSpace(result.Text) ? ChatPrompts.FallbackAnswer : result.Text;

        var botEntity = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conv.Id,
            Actor = ConversationActor.Bot,
            Content = botContent,
            CreatedUtc = botMsgTime,
            OrderIndex = baseOrder + 1
        };
        db.ConversationMessages.Add(botEntity);

        conv.UserMessageCount += 1;
        conv.LastMessageUtc = botMsgTime;

        await db.SaveChangesAsync(cancellationToken);

        return new ReplyDto(botEntity.Content, botEntity.Actor, botEntity.CreatedUtc);
    }

    private static List<LlmChatMessage> BuildLlmMessages(
        List<ConversationMessage> orderedHistory,
        List<ScoredContext> contextItems,
        string newQuestion)
    {
        // `orderedHistory` already contains the just-appended user message at the tail.
        // We rebuild the conversation so the LAST user turn carries the RAG context block.
        var list = new List<LlmChatMessage>(orderedHistory.Count + 1)
        {
            new(LlmChatRole.System, ChatPrompts.SystemPrompt)
        };

        for (var i = 0; i < orderedHistory.Count - 1; i++)
        {
            var m = orderedHistory[i];
            list.Add(new LlmChatMessage(
                m.Actor == ConversationActor.User ? LlmChatRole.User : LlmChatRole.Assistant,
                m.Content));
        }

        var tailContent = contextItems.Count > 0
            ? $"{BuildContextBlock(contextItems)}\n\nQuestion: {newQuestion}"
            : $"Question: {newQuestion}";
        list.Add(new LlmChatMessage(LlmChatRole.User, tailContent));
        return list;
    }

    private async Task DiagnoseAcquireFailureAsync(
        Guid userId, Guid conversationId, DateTime staleCutoff, CancellationToken cancellationToken)
    {
        var current = await db.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new { c.UserId, c.UserMessageCount, c.IsGenerating, c.GeneratingStartedUtc })
            .FirstOrDefaultAsync(cancellationToken);

        if (current is null || current.UserId != userId)
        {
            throw new NotFoundException("not-found", "Conversation not found.");
        }

        if (current.UserMessageCount >= _options.UserMessageCap)
        {
            throw new ConflictException("conversation-full",
                $"Conversation has reached the {_options.UserMessageCap}-message cap. Start a new conversation.");
        }

        if (current.IsGenerating && (current.GeneratingStartedUtc is null || current.GeneratingStartedUtc >= staleCutoff))
        {
            throw new ConflictException("conversation-busy",
                "Bot is still generating a reply for this conversation.");
        }

        // Race: lock was released between the acquire attempt and the diagnostic re-read.
        // Tell the caller to retry; surfaces as the same 409 they would have seen during the contention.
        throw new ConflictException("conversation-busy", "Lock acquisition raced; retry the request.");
    }

    private async Task ReleaseLockAsync(Guid conversationId)
    {
        // Use CancellationToken.None so caller-cancel cannot leave a stuck lock.
        await db.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsGenerating, false)
                .SetProperty(c => c.GeneratingStartedUtc, (DateTime?)null),
                CancellationToken.None);
    }

    private static string BuildQaText(RetrievedQA qa)
    {
        if (qa.Answers.Count == 0)
        {
            return $"Q: {qa.QuestionText}";
        }
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

    [LoggerMessage(EventId = 7101, Level = LogLevel.Information,
        Message = "Conversation retrieval: conversationId={ConversationId}, hitCount={HitCount}, threshold={Threshold}")]
    private static partial void LogRetrieved(ILogger logger, Guid conversationId, int hitCount, float threshold);

    [LoggerMessage(EventId = 7102, Level = LogLevel.Information,
        Message = "Conversation turn answered: conversationId={ConversationId}, promptTokens={PromptTokens}, completionTokens={CompletionTokens}, costCents={CostCents}")]
    private static partial void LogAnswered(ILogger logger, Guid conversationId, int promptTokens, int completionTokens, int costCents);

    private sealed record ScoredContext(string Text, float Score);
}
