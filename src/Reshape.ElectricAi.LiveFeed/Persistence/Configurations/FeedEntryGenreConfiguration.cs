using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Configurations;

internal sealed class FeedEntryGenreConfiguration : IEntityTypeConfiguration<FeedEntryGenre>
{
    public void Configure(EntityTypeBuilder<FeedEntryGenre> builder)
    {
        builder.ToTable("feed_entry_genres");
        builder.HasKey(g => new { g.FeedEntryId, g.Genre });
        builder.Property(g => g.Genre).HasConversion<string>().HasMaxLength(32);
    }
}
