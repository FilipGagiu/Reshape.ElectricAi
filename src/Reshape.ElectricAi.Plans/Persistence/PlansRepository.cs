using Reshape.ElectricAi.Infrastructure.Persistence;

namespace Reshape.ElectricAi.Plans.Persistence;

public sealed class PlansRepository<T>(PlansDbContext context) : EfRepository<PlansDbContext, T>(context)
    where T : class;
