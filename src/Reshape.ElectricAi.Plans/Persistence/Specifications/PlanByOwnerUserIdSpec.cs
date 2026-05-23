using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Specifications;

public sealed class PlanByOwnerUserIdSpec : Specification<Plan>
{
    public PlanByOwnerUserIdSpec(Guid ownerUserId)
    {
        Where(p => p.OwnerUserId == ownerUserId);
    }
}
