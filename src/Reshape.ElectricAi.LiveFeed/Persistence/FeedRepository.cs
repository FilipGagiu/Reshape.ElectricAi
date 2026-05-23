using Reshape.ElectricAi.Infrastructure.Persistence;

namespace Reshape.ElectricAi.LiveFeed.Persistence;

public sealed class FeedRepository<T>(FeedDbContext context)
    : EfRepository<FeedDbContext, T>(context)
    where T : class;
