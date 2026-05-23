using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Configurations;

internal sealed class FeedEntryConfiguration : IEntityTypeConfiguration<FeedEntry>
{
    public void Configure(EntityTypeBuilder<FeedEntry> builder)
    {
        builder.ToTable("feed_entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Body).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.PrimaryCategory).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.IsGeneral).IsRequired();
        builder.Property(e => e.PublishedByUserId).IsRequired();
        builder.Property(e => e.PublishedUtc).IsRequired();

        builder.HasIndex(e => e.PublishedUtc).IsDescending();
        // Partial index for the hot path "recent not-deleted entries" in RecentFeedEntriesSpec.
        // DeletedUtc ASC (NULL-first, then nothing because filter excludes non-NULL),
        // PublishedUtc DESC so the index can be scanned forward for newest-first reads.
        // Reviewer finding #30: without IsDescending(false, true) the PublishedUtc column
        // was indexed ASC and the most-recent-first scan path was sub-optimal.
        builder.HasIndex(e => new { e.DeletedUtc, e.PublishedUtc })
               .IsDescending(false, true)
               .HasFilter("\"DeletedUtc\" IS NULL");

        builder.HasMany(e => e.TargetArtists)
               .WithOne(a => a.FeedEntry!)
               .HasForeignKey(a => a.FeedEntryId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.TargetGenres)
               .WithOne(g => g.FeedEntry!)
               .HasForeignKey(g => g.FeedEntryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
