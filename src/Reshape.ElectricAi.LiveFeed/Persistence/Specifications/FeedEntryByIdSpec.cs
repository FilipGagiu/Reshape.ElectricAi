using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Specifications;

public sealed class FeedEntryByIdSpec : Specification<FeedEntry>
{
    // asNoTracking lets read-only callers (GetEntryByIdAsync) skip change-tracking overhead.
    // Update/Delete paths keep tracking on (default) so EF can mutate the loaded graph.
    // Reviewer finding #18.
    public FeedEntryByIdSpec(Guid entryId, bool asNoTracking = false)
    {
        Where(e => e.Id == entryId);
        AddInclude(e => e.TargetArtists);
        AddInclude(e => e.TargetGenres);
        EnableSplitQuery();
        if (asNoTracking)
        {
            EnableNoTracking();
        }
    }
}
