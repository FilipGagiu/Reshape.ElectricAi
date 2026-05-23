using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Specifications;

public sealed class FeedEntriesSinceCursorSpec : Specification<FeedEntry>
{
    public FeedEntriesSinceCursorSpec(DateTime cursorPublishedUtc, Guid cursorEntryId, int take)
    {
        Where(e => e.PublishedUtc > cursorPublishedUtc
                || (e.PublishedUtc == cursorPublishedUtc && e.Id > cursorEntryId));

        AddInclude(e => e.TargetArtists);
        AddInclude(e => e.TargetGenres);
        ApplyOrderBy(e => e.PublishedUtc);
        ApplyPaging(0, take);
        EnableNoTracking();
        EnableSplitQuery();
    }
}
