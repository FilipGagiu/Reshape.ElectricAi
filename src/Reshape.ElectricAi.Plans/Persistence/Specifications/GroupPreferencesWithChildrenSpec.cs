using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Specifications;

public sealed class GroupPreferencesWithChildrenSpec : Specification<GroupPreferences>
{
    public GroupPreferencesWithChildrenSpec(Guid groupId)
    {
        Where(p => p.GroupId == groupId);
        AddInclude(p => p.Genres);
        AddInclude(p => p.FoodRestrictions);
        AddInclude(p => p.Activities);
        AddInclude(p => p.Artists);
        AddInclude(p => p.Cuisines);
        EnableSplitQuery();
    }
}
