using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Specifications;

public sealed class RecentFeedEntriesSpec : Specification<FeedEntry>
{
    public RecentFeedEntriesSpec(Category? categoryFilter, int take)
    {
        if (categoryFilter is { } cat)
            Where(e => e.PrimaryCategory == cat);

        AddInclude(e => e.TargetArtists);
        AddInclude(e => e.TargetGenres);
        ApplyOrderByDescending(e => e.PublishedUtc);
        ApplyPaging(0, take);
        EnableNoTracking();
        EnableSplitQuery();
    }
}
