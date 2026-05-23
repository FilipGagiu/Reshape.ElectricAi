using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Specifications;

public sealed class UserPreferencesWithChildrenSpec : Specification<UserPreferences>
{
    public UserPreferencesWithChildrenSpec(Guid userId)
    {
        Where(p => p.UserId == userId);
        AddInclude(p => p.Genres);
        AddInclude(p => p.FoodRestrictions);
        AddInclude(p => p.Activities);
        AddInclude(p => p.Artists);
        AddInclude(p => p.Cuisines);
        EnableSplitQuery();
    }
}
