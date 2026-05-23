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

internal sealed class FeedService(
    IRepository<FeedEntry> repository,
    IFeedBroadcaster broadcaster) : IFeedService
{
    public async Task<FeedEntryDto> PublishEntryAsync(
        Guid organizerId, PublishFeedEntryCommand command, CancellationToken ct)
    {
        var entry = command.ToNewEntity(organizerId);
        await repository.AddAsync(entry, ct);
        await repository.SaveChangesAsync(ct);

        var dto = entry.ToDto();
        broadcaster.BroadcastEventToMatchingSubscribers(FeedEventKind.Created, dto);
        return dto;
    }

    public async Task<FeedEntryDto> UpdateEntryByIdAsync(
        Guid entryId, UpdateFeedEntryCommand command, CancellationToken ct)
    {
        var entry = await repository.FirstOrDefaultAsync(new FeedEntryByIdSpec(entryId), ct)
            ?? throw new NotFoundException("feed-entry-not-found", $"Feed entry {entryId} not found");

        if (entry.DeletedUtc is not null)
            throw new NotFoundException("feed-entry-not-found", $"Feed entry {entryId} is deleted");

        command.ApplyUpdateTo(entry);
        repository.Update(entry);
        await repository.SaveChangesAsync(ct);

        var dto = entry.ToDto();
        broadcaster.BroadcastEventToMatchingSubscribers(FeedEventKind.Updated, dto);
        return dto;
    }

    public async Task SoftDeleteEntryByIdAsync(Guid entryId, CancellationToken ct)
    {
        var entry = await repository.FirstOrDefaultAsync(new FeedEntryByIdSpec(entryId), ct);
        if (entry is null || entry.DeletedUtc is not null)
            return; // idempotent: no-op, no broadcast

        entry.DeletedUtc = DateTime.UtcNow;
        repository.Update(entry);
        await repository.SaveChangesAsync(ct);

        broadcaster.BroadcastEventToMatchingSubscribers(FeedEventKind.Deleted, entry.ToDto());
    }

    public async Task<FeedEntryDto?> GetEntryByIdAsync(Guid entryId, CancellationToken ct)
    {
        var entry = await repository.FirstOrDefaultAsync(new FeedEntryByIdSpec(entryId, asNoTracking: true), ct);
        if (entry is null || entry.DeletedUtc is not null) return null;
        return entry.ToDto();
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
