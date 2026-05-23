using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Configurations;

internal sealed class FeedEntryArtistConfiguration : IEntityTypeConfiguration<FeedEntryArtist>
{
    public void Configure(EntityTypeBuilder<FeedEntryArtist> builder)
    {
        builder.ToTable("feed_entry_artists");
        builder.HasKey(a => new { a.FeedEntryId, a.ArtistName });
        builder.Property(a => a.ArtistName).HasMaxLength(100);
    }
}
