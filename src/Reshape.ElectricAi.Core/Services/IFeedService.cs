using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Services;

public interface IFeedService
{
    Task<FeedEntryDto> PublishEntryAsync(
        Guid organizerId, PublishFeedEntryCommand command, CancellationToken ct);

    Task<FeedEntryDto> UpdateEntryByIdAsync(
        Guid entryId, UpdateFeedEntryCommand command, CancellationToken ct);

    Task SoftDeleteEntryByIdAsync(Guid entryId, CancellationToken ct);

    Task<FeedEntryDto?> GetEntryByIdAsync(Guid entryId, CancellationToken ct);

    Task<IReadOnlyList<FeedEntryDto>> ListRecentEntriesMatchingPrefsAsync(
        UserFeedPrefs prefs, Category? categoryFilter, int take, CancellationToken ct);

    Task<IReadOnlyList<FeedEntryDto>> ListEntriesSinceEventIdMatchingPrefsAsync(
        string lastEventId, UserFeedPrefs prefs, int take, CancellationToken ct);
}
