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
    IItineraryRefiner refiner,
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

        var persistedItinerary = await UpsertPlanAsync(userId, itinerary, cancellationToken);

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
            persistedItinerary = await RetryGenerateSaveAsync(userId, itinerary, cancellationToken);
            LogGenerateCompleted(logger, userId, persistedItinerary.Sections.Count);
            return new ItineraryResponse(prefsEntity.ToDto(), persistedItinerary);
        }

        await tx.CommitAsync(cancellationToken);

        LogGenerateCompleted(logger, userId, persistedItinerary.Sections.Count);
        return new ItineraryResponse(prefsEntity.ToDto(), persistedItinerary);
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

        var itinerary = DeserializeWithIdHeal(plan);

        // prefs.ToDto() tolerates null and returns an empty-default DTO.
        var prefs = await prefsRepository.FirstOrDefaultAsync(
            new UserPreferencesWithChildrenSpec(userId), cancellationToken);
        return new ItineraryResponse(prefs.ToDto(), itinerary);
    }

    public async Task<Guid?> GetLatestIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Direct dbContext (not IRepository) because the Specification pattern in this codebase
        // doesn't model projections, and we want the cheap path that skips loading ContentJson.
        return await dbContext.Plans
            .AsNoTracking()
            .Where(p => p.OwnerUserId == userId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ItineraryResponse?> GetByIdAsync(Guid itineraryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var plan = await planRepository.FirstOrDefaultAsync(
            new PlanByIdSpec(itineraryId), cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var itinerary = DeserializeWithIdHeal(plan);

        // No owner check — v1 decision: any logged-in user may read any itinerary by Id.
        // Prefs are returned for the row's owning user, not the caller.
        var prefs = await prefsRepository.FirstOrDefaultAsync(
            new UserPreferencesWithChildrenSpec(plan.OwnerUserId), cancellationToken);
        return new ItineraryResponse(prefs.ToDto(), itinerary);
    }

    private static ItineraryDto DeserializeWithIdHeal(Plan plan)
    {
        var itinerary = JsonSerializer.Deserialize<ItineraryDto>(plan.ContentJson, LlmJsonOptions.Default)
            ?? throw new InvalidOperationException(
                $"Plan {plan.Id} ContentJson failed to deserialize to ItineraryDto.");
        // Legacy rows written before ItineraryDto.Id existed deserialize with Guid.Empty.
        // Materialize the row's Id at read time; the next write rewrites ContentJson with it.
        return itinerary.Id == Guid.Empty ? itinerary with { Id = plan.Id } : itinerary;
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

        var persistedItinerary = await UpsertPlanAsync(userId, itinerary, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Same race condition as GenerateAsync: a concurrent generate inserted a Plan
            // row between our FirstOrDefault and SaveChanges. Retry with Update.
            persistedItinerary = await RetryRebuildSaveAsync(userId, itinerary, cancellationToken);
        }

        LogRebuildCompleted(logger, userId, persistedItinerary.Sections.Count);
        return new ItineraryResponse(prefsEntity.ToDto(), persistedItinerary);
    }

    // Stamps ItineraryDto.Id with the Plan row's Id (reusing existing on update, newly minted on insert)
    // BEFORE serializing into ContentJson, so the jsonb-stored copy round-trips identically. Returns the
    // stamped dto so callers can put the same Id on the wire.
    private async Task<ItineraryDto> UpsertPlanAsync(Guid userId, ItineraryDto itinerary, CancellationToken cancellationToken)
    {
        var existing = await planRepository.FirstOrDefaultAsync(
            new PlanByOwnerUserIdSpec(userId), cancellationToken);
        var planId = existing?.Id ?? Guid.NewGuid();
        var stamped = itinerary with { Id = planId };
        var contentJson = JsonSerializer.Serialize(stamped, LlmJsonOptions.Default);

        if (existing is null)
        {
            var newPlan = new Plan
            {
                Id = planId,
                OwnerUserId = userId,
                ContentJson = contentJson,
                GeneratedUtc = stamped.GeneratedUtc
            };
            await planRepository.AddAsync(newPlan, cancellationToken);
        }
        else
        {
            existing.ContentJson = contentJson;
            existing.GeneratedUtc = stamped.GeneratedUtc;
            planRepository.Update(existing);
        }

        return stamped;
    }

    private async Task<ItineraryDto> RetryGenerateSaveAsync(Guid userId, ItineraryDto itinerary, CancellationToken cancellationToken)
    {
        // Drop the loser's failed Add from the change tracker before re-reading. Otherwise EF's
        // identity resolution returns the stale Added entity from FirstOrDefault and we never see
        // the winner's row.
        dbContext.ChangeTracker.Clear();
        // Open a fresh tx and force an Update path. Re-read prefs to merge against the latest row
        // (the concurrent winner may have already applied a different extraction).
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var stamped = await UpsertPlanAsync(userId, itinerary, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return stamped;
    }

    private async Task<ItineraryDto> RetryRebuildSaveAsync(Guid userId, ItineraryDto itinerary, CancellationToken cancellationToken)
    {
        // Same defensive ChangeTracker.Clear as RetryGenerateSaveAsync (no tx wrap because the
        // rebuild path is single-write; the prefs save already committed in PreferencesController).
        dbContext.ChangeTracker.Clear();
        var stamped = await UpsertPlanAsync(userId, itinerary, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return stamped;
    }

    public async Task<ItineraryResponse> RefineAsync(
        Guid userId,
        ItineraryRefineRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var plan = await planRepository.FirstOrDefaultAsync(
            new PlanByOwnerUserIdSpec(userId), cancellationToken);
        if (plan is null || plan.Id != request.ItineraryId)
        {
            // Same code for "no plan" and "wrong id" — do not leak whether the id exists for another user.
            throw new NotFoundException("itinerary-not-found", "Itinerary does not exist.");
        }

        var prefsEntity = await prefsRepository.FirstOrDefaultAsync(
            new UserPreferencesWithChildrenSpec(userId), cancellationToken);
        if (prefsEntity is null)
        {
            // Defensive — a Plan implies a UserPreferences row was written during generate.
            // Reachable only if the prefs row was admin-deleted out of band.
            throw new NotFoundException("preferences-not-found", "No preferences row for this user.");
        }

        var snapshot = UserPreferencesSnapshotFactory.FromEntity(prefsEntity);
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "en" : request.Locale;
        LogRefineStarted(logger, userId, request.FreeText.Length, locale);

        // Paid LLM call BEFORE opening the DB transaction.
        var extracted = await refiner.RefineAsync(snapshot, request.FreeText, locale, cancellationToken);

        var nowUtc = DateTime.UtcNow;
        prefsEntity.ApplyExtracted(extracted, nowUtc);

        var newSnapshot = UserPreferencesSnapshotFactory.FromEntity(prefsEntity);
        // Vector + DB lookups OUTSIDE the Plans tx (separate VectorDbContext).
        var itinerary = await builder.BuildAsync(newSnapshot, cancellationToken);

        // Single tx wraps the two single-row writes (UserPreferences update + Plan update).
        // No unique-violation retry needed — we already loaded the Plan row, so UpsertPlanAsync
        // deterministically hits the Update branch.
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var persistedItinerary = await UpsertPlanAsync(userId, itinerary, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        LogRefineCompleted(logger, userId, persistedItinerary.Sections.Count);
        return new ItineraryResponse(prefsEntity.ToDto(), persistedItinerary);
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

    [LoggerMessage(EventId = 7204, Level = LogLevel.Information,
        Message = "ItineraryRefine started userId={UserId} freeTextLength={FreeTextLength} locale={Locale}")]
    private static partial void LogRefineStarted(ILogger logger, Guid userId, int freeTextLength, string locale);

    [LoggerMessage(EventId = 7205, Level = LogLevel.Information,
        Message = "ItineraryRefine completed userId={UserId} sections={Sections}")]
    private static partial void LogRefineCompleted(ILogger logger, Guid userId, int sections);
}
