using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Plans;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Configuration;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Extensions;
using Reshape.ElectricAi.Plans.Persistence.Specifications;
using Reshape.ElectricAi.Plans.Services.Generation;

namespace Reshape.ElectricAi.Plans.Services;

internal sealed partial class PlanGenerator(
    IOpenAiClient openAi,
    IRepository<User> userRepository,
    IRepository<UserPreferences> prefsRepository,
    IRepository<Plan> planRepository,
    IRateLimiter rateLimiter,
    Reshape.ElectricAi.Plans.Persistence.PlansDbContext dbContext,
    IOptions<PlanGenerationOptions> options,
    ILogger<PlanGenerator> logger) : IPlanGenerator
{
    private static readonly string SystemPrompt = LoadEmbeddedPrompt();
    private static readonly JsonNode ResponseSchema = JsonSchemaExporter.GetJsonSchemaAsNode(
        LlmJsonOptions.Default,
        typeof(AiPlanEnvelope));

    private readonly PlanGenerationOptions _options = options.Value;

    public async Task<PlanGenerationResult> GenerateAsync(
        Guid userId,
        PlanGenerationRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var window = new RateLimitWindow(_options.RateLimit.PerHour, TimeSpan.FromHours(1));
        await rateLimiter.AcquireAsync($"plan-gen:{userId}", window, cancellationToken);

        var userExists = await userRepository.AnyAsync(new UserExistsSpec(userId), cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException("user-not-found", "User does not exist.");
        }

        LogStarted(logger, userId, request.Answers.Count, request.FreeText?.Length ?? 0);
        var userPrompt = BuildUserPrompt(request);

        var llm = await openAi.CompleteStructuredAsync<AiPlanEnvelope>(
            SystemPrompt,
            userPrompt,
            ResponseSchema,
            _options.Model,
            _options.MaxCompletionTokens,
            _options.Temperature,
            cancellationToken);

        ValidateEnvelope(llm.Value);

        // Atomic persistence: prefs upsert + plan insert commit together or roll back together.
        // The paid LLM call has already completed — never leave half the result on disk.
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var prefsEntity = await UpsertPreferencesAsync(userId, llm.Value.Preferences, cancellationToken);
        var sanitizedTip = SanitizeTip(llm.Value.Tip, userId);

        var planId = Guid.NewGuid();
        var resolvedTicketType = llm.Value.Plan.TicketType ?? prefsEntity.TicketType;
        if (resolvedTicketType is null)
        {
            LogTicketTypeFallback(logger, userId);
        }
        var planTicketType = resolvedTicketType ?? TicketType.Standard;
        var planDto = new PlanDto(
            Id: planId,
            Scope: PlanScope.Individual,
            State: PlanState.Ready,
            TicketType: planTicketType,
            Days: llm.Value.Plan.Days ?? new List<PlanDayDto>(),
            Food: llm.Value.Plan.Food ?? new List<PlanFoodDto>(),
            Budget: llm.Value.Plan.Budget ?? new PlanBudgetDto(0, 0, 0, 0, 0, 0, 0, "RON-cents"),
            ExportedUtc: null);

        var planEntity = new Plan
        {
            Id = planId,
            Scope = PlanScope.Individual,
            OwnerUserId = userId,
            GroupId = null,
            TicketType = planTicketType,
            ContentJson = planDto.SerializeContent(),
            Tip = sanitizedTip,
            GeneratedUtc = DateTime.UtcNow
        };
        await planRepository.AddAsync(planEntity, cancellationToken);

        // Single flush — covers prefs upsert + plan insert in one round-trip.
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        LogCompleted(logger, userId, planId, llm.Usage.PromptTokens, llm.Usage.CompletionTokens, llm.Usage.CostCents);

        var prefsDto = prefsEntity.ToDto();
        return new PlanGenerationResult(planDto, prefsDto, sanitizedTip);
    }

    private async Task<UserPreferences> UpsertPreferencesAsync(
        Guid userId,
        AiPreferences ai,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var entity = await prefsRepository.FirstOrDefaultAsync(
            new UserPreferencesWithChildrenSpec(userId), cancellationToken);

        var replaceRequest = new PreferencesReplaceRequest(
            TicketType: ai.TicketType,
            Accommodation: ai.Accommodation,
            Transport: ai.Transport,
            AgeGroup: ai.AgeGroup,
            MusicGenres: ai.MusicGenres,
            FoodRestrictions: ai.FoodRestrictions,
            Activities: ai.Activities,
            Artists: ai.Artists,
            Cuisines: ai.Cuisines);

        if (entity is null)
        {
            entity = new UserPreferences { UserId = userId, UpdatedUtc = nowUtc };
            entity.ApplyReplace(replaceRequest, nowUtc);
            await prefsRepository.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.ApplyReplace(replaceRequest, nowUtc);
        }

        return entity;
    }

    private string SanitizeTip(string tip, Guid userId)
    {
        var sanitized = tip
            .Replace("—", "-", StringComparison.Ordinal)   // em-dash U+2014
            .Replace("–", "-", StringComparison.Ordinal);  // en-dash U+2013
        if (!string.Equals(sanitized, tip, StringComparison.Ordinal))
        {
            var replacements = tip.Count(c => c == '—' || c == '–');
            LogTipSanitized(logger, userId, replacements);
        }
        return sanitized;
    }

    private static void ValidateEnvelope(AiPlanEnvelope env)
    {
        if (env.Preferences is null)
        {
            throw new LlmSchemaException("preferences");
        }
        if (env.Plan is null)
        {
            throw new LlmSchemaException("plan");
        }
        if (env.Plan.Days is null || env.Plan.Days.Count == 0)
        {
            throw new LlmSchemaException("plan.days");
        }
        if (env.Plan.Food is null)
        {
            throw new LlmSchemaException("plan.food");
        }
        if (env.Plan.Budget is null || env.Plan.Budget.Total <= 0)
        {
            throw new LlmSchemaException("plan.budget");
        }
        if (string.IsNullOrWhiteSpace(env.Tip) || env.Tip.Length < 10)
        {
            throw new LlmSchemaException("tip");
        }
    }

    private static string BuildUserPrompt(PlanGenerationRequest request)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("User answered the wizard like this:");
        sb.AppendLine();
        var i = 1;
        foreach (var a in request.Answers)
        {
            sb.AppendLine(ci, $"{i++}. {a.QuestionText}");
            sb.AppendLine(ci, $"   -> {a.Answer}");
            sb.AppendLine();
        }
        sb.AppendLine("Additional notes from the user:");
        sb.AppendLine(string.IsNullOrWhiteSpace(request.FreeText) ? "(none)" : request.FreeText);
        sb.AppendLine();
        sb.AppendLine("Now produce the preferences, plan, and tip via the response tool.");
        return sb.ToString();
    }

    private static string LoadEmbeddedPrompt()
    {
        var asm = typeof(PlanGenerator).Assembly;
        const string resourceName = "Reshape.ElectricAi.Plans.Services.Prompts.PlanGeneratorSystemPrompt.md";
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource missing: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [LoggerMessage(EventId = 6001, Level = LogLevel.Information,
        Message = "PlanGeneration started userId={UserId}, answerCount={AnswerCount}, freeTextLength={FreeTextLength}")]
    private static partial void LogStarted(ILogger logger, Guid userId, int answerCount, int freeTextLength);

    [LoggerMessage(EventId = 6002, Level = LogLevel.Information,
        Message = "PlanGeneration completed userId={UserId}, planId={PlanId}, promptTokens={PromptTokens}, completionTokens={CompletionTokens}, costCents={CostCents}")]
    private static partial void LogCompleted(ILogger logger, Guid userId, Guid planId, int promptTokens, int completionTokens, int costCents);

    [LoggerMessage(EventId = 6003, Level = LogLevel.Warning,
        Message = "Plan tip sanitized userId={UserId}, replacements={Replacements}")]
    private static partial void LogTipSanitized(ILogger logger, Guid userId, int replacements);

    [LoggerMessage(EventId = 6004, Level = LogLevel.Warning,
        Message = "PlanGeneration ticket-type fallback fired (LLM omitted + no prefs row) userId={UserId}, fallback=Standard")]
    private static partial void LogTicketTypeFallback(ILogger logger, Guid userId);
}
