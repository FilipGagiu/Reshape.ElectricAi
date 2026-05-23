using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Specifications;

public sealed class EventByFeedEntryIdSpec : Specification<EventEntry>
{
    public EventByFeedEntryIdSpec(Guid feedEntryId, bool asNoTracking = true)
    {
        Where(e => e.FeedEntryId == feedEntryId);
        if (asNoTracking) EnableNoTracking();
    }
}
