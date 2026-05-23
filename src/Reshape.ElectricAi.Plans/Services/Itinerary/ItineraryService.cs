using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Itinerary;
using Reshape.ElectricAi.Plans.Configuration;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Extensions;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Persistence.Specifications;

namespace Reshape.ElectricAi.Plans.Services.Itinerary;

internal sealed partial class ItineraryService(
    IPreferencesExtractor extractor,
    IItineraryBuilder builder,
    IRepository<User> userRepository,
    IRepository<UserPreferences> prefsRepository,
    IRepository<Plan> planRepository,
    PlansDbContext dbContext,
    IRateLimiter rateLimiter,
    IOptions<ItineraryGenerationOptions> options,
    ILogger<ItineraryService> logger) : IItineraryService
{
    private readonly ItineraryGenerationOptions _options = options.Value;

    public async Task<ItineraryResponse> GenerateAsync(
        Guid userId,
        ItineraryGenerationRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var generationWindow = new RateLimitWindow(_options.RateLimit.PerHour, TimeSpan.FromHours(1));
        await rateLimiter.AcquireAsync($"itinerary-gen:{userId}", generationWindow, cancellationToken);

        var userExists = await userRepository.AnyAsync(new UserExistsSpec(userId), cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException("user-not-found", "User does not exist.");
        }

        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "en" : request.Locale;
        LogGenerateStarted(logger, userId, request.Answers.Count, request.FreeText?.Length ?? 0, locale);

        // Paid LLM call BEFORE opening the DB transaction.
        var extracted = await extractor.ExtractAsync(request.Answers, request.FreeText, locale, cancellationToken);

        // Build a transient prefs entity to feed the section pipeline. We deliberately keep the
        // expensive vector-search/event-lookup calls OUTSIDE the Plans transaction so the
        // connection isn't held idle during embedding + ILIKE queries on the VectorDb context.
        var nowUtc = DateTime.UtcNow;
        var existingPrefs = await prefsRepository.FirstOrDefaultAsync(
            new UserPreferencesWithChildrenSpec(userId), cancellationToken);
        var prefsEntity = existingPrefs ?? new UserPreferences { UserId = userId, UpdatedUtc = nowUtc };
        prefsEntity.ApplyExtracted(extracted, nowUtc);

        var snapshot = UserPreferencesSnapshotFactory.FromEntity(prefsEntity);
        var itinerary = await builder.BuildAsync(snapshot, cancellationToken);

        // Persist prefs + snapshot atomically. The LLM + vector work is already done; this tx is
        // just two single-row writes (UserPreferences + Plan).
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (existingPrefs is null)
        {
            await prefsRepository.AddAsync(prefsEntity, cancellationToken);
        }

        await UpsertPlanAsync(userId, itinerary, cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Race: concurrent /itinerary/generate for the same user. The unique index on
            // Plans.OwnerUserId serializes; the loser retries by re-reading + updating.
            // Detach the failed Plan so we can re-load and Update instead of Add.
            await tx.RollbackAsync(cancellationToken);
            await RetryGenerateSaveAsync(userId, itinerary, cancellationToken);
            LogGenerateCompleted(logger, userId, itinerary.Sections.Count);
            return new ItineraryResponse(prefsEntity.ToDto(), itinerary);
        }

        await tx.CommitAsync(cancellationToken);

        LogGenerateCompleted(logger, userId, itinerary.Sections.Count);
        return new ItineraryResponse(prefsEntity.ToDto(), itinerary);
    }

    public async Task<ItineraryResponse?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var plan = await planRepository.FirstOrDefaultAsync(
            new PlanByOwnerUserIdSpec(userId), cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var itinerary = JsonSerializer.Deserialize<ItineraryDto>(plan.ContentJson, LlmJsonOptions.Default)
            ?? throw new InvalidOperationException(
                $"Plan {plan.Id} ContentJson failed to deserialize to ItineraryDto.");

        // prefs.ToDto() tolerates null and returns an empty-default DTO.
        var prefs = await prefsRepository.FirstOrDefaultAsync(
            new UserPreferencesWithChildrenSpec(userId), cancellationToken);
        return new ItineraryResponse(prefs.ToDto(), itinerary);
    }

    public async Task<ItineraryResponse> RebuildAfterPrefsChangeAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Independent rate limit on the no-LLM rebuild path so a user can't spam
        // PUT/PATCH /preferences and amplify each call into N VectorDb queries.
        var rebuildWindow = new RateLimitWindow(_options.PrefsRateLimit.PerHour, TimeSpan.FromHours(1));
        await rateLimiter.AcquireAsync($"prefs-update:{userId}", rebuildWindow, cancellationToken);

        var prefsEntity = await prefsRepository.FirstOrDefaultAsync(
            new UserPreferencesWithChildrenSpec(userId), cancellationToken);
        if (prefsEntity is null)
        {
            throw new NotFoundException("preferences-not-found", "No preferences row for this user.");
        }

        var snapshot = UserPreferencesSnapshotFactory.FromEntity(prefsEntity);
        var itinerary = await builder.BuildAsync(snapshot, cancellationToken);

        await UpsertPlanAsync(userId, itinerary, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Same race condition as GenerateAsync: a concurrent generate inserted a Plan
            // row between our FirstOrDefault and SaveChanges. Retry with Update.
            await RetryRebuildSaveAsync(userId, itinerary, cancellationToken);
        }

        LogRebuildCompleted(logger, userId, itinerary.Sections.Count);
        return new ItineraryResponse(prefsEntity.ToDto(), itinerary);
    }

    private async Task UpsertPlanAsync(Guid userId, ItineraryDto itinerary, CancellationToken cancellationToken)
    {
        var contentJson = JsonSerializer.Serialize(itinerary, LlmJsonOptions.Default);
        var existing = await planRepository.FirstOrDefaultAsync(
            new PlanByOwnerUserIdSpec(userId), cancellationToken);

        if (existing is null)
        {
            var newPlan = new Plan
            {
                Id = Guid.NewGuid(),
                OwnerUserId = userId,
                ContentJson = contentJson,
                GeneratedUtc = itinerary.GeneratedUtc
            };
            await planRepository.AddAsync(newPlan, cancellationToken);
        }
        else
        {
            existing.ContentJson = contentJson;
            existing.GeneratedUtc = itinerary.GeneratedUtc;
            planRepository.Update(existing);
        }
    }

    private async Task RetryGenerateSaveAsync(Guid userId, ItineraryDto itinerary, CancellationToken cancellationToken)
    {
        // Open a fresh tx and force an Update path. Re-read prefs to merge against the latest row
        // (the concurrent winner may have already applied a different extraction).
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await UpsertPlanAsync(userId, itinerary, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async Task RetryRebuildSaveAsync(Guid userId, ItineraryDto itinerary, CancellationToken cancellationToken)
    {
        await UpsertPlanAsync(userId, itinerary, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(EventId = 7201, Level = LogLevel.Information,
        Message = "ItineraryGenerate started userId={UserId} answers={Answers} freeTextLength={FreeTextLength} locale={Locale}")]
    private static partial void LogGenerateStarted(ILogger logger, Guid userId, int answers, int freeTextLength, string locale);

    [LoggerMessage(EventId = 7202, Level = LogLevel.Information,
        Message = "ItineraryGenerate completed userId={UserId} sections={Sections}")]
    private static partial void LogGenerateCompleted(ILogger logger, Guid userId, int sections);

    [LoggerMessage(EventId = 7203, Level = LogLevel.Information,
        Message = "ItineraryRebuild completed userId={UserId} sections={Sections}")]
    private static partial void LogRebuildCompleted(ILogger logger, Guid userId, int sections);
}
