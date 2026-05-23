using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Specifications;

public sealed class UserByEmailSpec : Specification<User>
{
    public UserByEmailSpec(string normalizedEmail)
    {
        Where(u => u.Email == normalizedEmail);
    }
}
