using Reshape.ElectricAi.Infrastructure.Persistence;

namespace Reshape.ElectricAi.VectorDb.Persistence;

public sealed class VectorRepository<T>(VectorDbContext context) : EfRepository<VectorDbContext, T>(context)
    where T : class;
