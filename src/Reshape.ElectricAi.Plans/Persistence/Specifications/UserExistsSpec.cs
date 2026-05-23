using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Specifications;

public sealed class UserExistsSpec : Specification<User>
{
    public UserExistsSpec(Guid userId)
    {
        Where(u => u.Id == userId);
    }
}
