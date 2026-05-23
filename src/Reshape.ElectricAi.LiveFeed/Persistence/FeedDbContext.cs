using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence;

public class FeedDbContext(DbContextOptions<FeedDbContext> options) : DbContext(options)
{
    public DbSet<FeedEntry> FeedEntries => Set<FeedEntry>();
    public DbSet<FeedEntryArtist> FeedEntryArtists => Set<FeedEntryArtist>();
    public DbSet<FeedEntryGenre> FeedEntryGenres => Set<FeedEntryGenre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("feed");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeedDbContext).Assembly);
    }
}
