using Microsoft.Extensions.Logging;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Broadcasting;
using Reshape.ElectricAi.LiveFeed.Dtos.Mapping;
using Reshape.ElectricAi.LiveFeed.Entities;
using Reshape.ElectricAi.LiveFeed.Persistence.Specifications;

namespace Reshape.ElectricAi.LiveFeed.Services;

internal sealed partial class FeedService(
    IRepository<FeedEntry> repository,
    IFeedBroadcaster broadcaster,
    IIngestService ingestService,
    ILogger<FeedService> logger) : IFeedService
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "Vector indexing failed for FeedEntry {FeedEntryId} after publish; entry is committed and broadcast, vector index will be stale until a future re-ingest.")]
    private partial void LogVectorIndexingFailed(Guid feedEntryId, Exception ex);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Vector removal failed for FeedEntry {FeedEntryId} after delete; entry is gone from feed, vector index row will be stale until a future cleanup.")]
    private partial void LogVectorRemovalFailed(Guid feedEntryId, Exception ex);

    public async Task<FeedEntryDto> PublishEntryAsync(
        Guid organizerId, PublishFeedEntryCommand command, CancellationToken ct)
    {
        var entry = command.ToNewEntity(organizerId);
        await repository.AddAsync(entry, ct);
        await repository.SaveChangesAsync(ct);

        var dto = entry.ToDto();
        broadcaster.BroadcastEventToMatchingSubscribers(FeedEventKind.Created, dto);

        await SafeIngestEventAsync(entry, ct);

        return dto;
    }

    private async Task SafeIngestEventAsync(FeedEntry entry, CancellationToken ct)
    {
        try
        {
            await ingestService.IngestEventAsync(entry.ToIngestEventRequest(), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogVectorIndexingFailed(entry.Id, ex);
        }
    }

    public async Task<FeedEntryDto> UpdateEntryByIdAsync(
        Guid entryId, UpdateFeedEntryCommand command, CancellationToken ct)
    {
        var entry = await repository.FirstOrDefaultAsync(new FeedEntryByIdSpec(entryId), ct)
            ?? throw new NotFoundException("feed-entry-not-found", $"Feed entry {entryId} not found");

        command.ApplyUpdateTo(entry);
        repository.Update(entry);
        await repository.SaveChangesAsync(ct);

        var dto = entry.ToDto();
        broadcaster.BroadcastEventToMatchingSubscribers(FeedEventKind.Updated, dto);
        return dto;
    }

    public async Task DeleteEntryByIdAsync(Guid entryId, CancellationToken ct)
    {
        var entry = await repository.FirstOrDefaultAsync(new FeedEntryByIdSpec(entryId), ct);
        if (entry is null)
            return; // idempotent: already gone, no broadcast

        // Project the DTO BEFORE Remove — entity collections detach on remove,
        // and the broadcast envelope needs the full picture (target artists/genres).
        var dto = entry.ToDto();

        repository.Remove(entry);
        await repository.SaveChangesAsync(ct);

        broadcaster.BroadcastEventToMatchingSubscribers(FeedEventKind.Deleted, dto);
        await SafeRemoveEventAsync(entry.Id, ct);
    }

    private async Task SafeRemoveEventAsync(Guid feedEntryId, CancellationToken ct)
    {
        try
        {
            await ingestService.RemoveEventAsync(feedEntryId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogVectorRemovalFailed(feedEntryId, ex);
        }
    }

    public async Task<FeedEntryDto?> GetEntryByIdAsync(Guid entryId, CancellationToken ct)
    {
        var entry = await repository.FirstOrDefaultAsync(new FeedEntryByIdSpec(entryId, asNoTracking: true), ct);
        return entry?.ToDto();
    }

    public async Task<IReadOnlyList<FeedEntryDto>> ListRecentEntriesMatchingPrefsAsync(
        UserFeedPrefs prefs, Category? categoryFilter, int take, CancellationToken ct)
    {
        var entries = await repository.ListAsync(new RecentFeedEntriesSpec(categoryFilter, take), ct);
        return entries
            .Select(e => e.ToDto())
            .Where(dto => FeedTargeting.EntryMatchesUserPrefs(dto, prefs))
            .ToList();
    }

    public async Task<IReadOnlyList<FeedEntryDto>> ListEntriesSinceEventIdMatchingPrefsAsync(
        string lastEventId, UserFeedPrefs prefs, int take, CancellationToken ct)
    {
        if (!FeedEventId.TryParseEntryIdFromEventId(lastEventId, out var cursorId, out var cursorUtc))
            return await ListRecentEntriesMatchingPrefsAsync(prefs, null, take, ct);

        var entries = await repository.ListAsync(new FeedEntriesSinceCursorSpec(cursorUtc, cursorId, take), ct);
        return entries
            .Select(e => e.ToDto())
            .Where(dto => FeedTargeting.EntryMatchesUserPrefs(dto, prefs))
            .ToList();
    }
}
