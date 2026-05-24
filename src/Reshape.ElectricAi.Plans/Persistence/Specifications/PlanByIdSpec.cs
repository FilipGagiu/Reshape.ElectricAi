using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Specifications;

public sealed class PlanByIdSpec : Specification<Plan>
{
    public PlanByIdSpec(Guid id)
    {
        Where(p => p.Id == id);
    }
}
