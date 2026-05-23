using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Specifications;

public sealed class FeedEntryByIdSpec : Specification<FeedEntry>
{
    public FeedEntryByIdSpec(Guid entryId)
    {
        Where(e => e.Id == entryId);
        AddInclude(e => e.TargetArtists);
        AddInclude(e => e.TargetGenres);
        EnableSplitQuery();
    }
}
