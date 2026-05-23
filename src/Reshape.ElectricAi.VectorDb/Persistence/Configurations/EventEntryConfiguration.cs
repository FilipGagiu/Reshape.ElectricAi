using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Configurations;

internal sealed class EventEntryConfiguration : IEntityTypeConfiguration<EventEntry>
{
    public void Configure(EntityTypeBuilder<EventEntry> builder)
    {
        builder.ToTable("event_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.TextRepresentation)
            .IsRequired();

        builder.HasIndex(e => e.FeedEntryId)
            .IsUnique();

        builder.Property(e => e.CategoryTags)
            .HasColumnType("text[]");

        builder.HasIndex(e => e.CategoryTags)
            .HasMethod("gin");

        builder.Property(e => e.EventUtc)
            .IsRequired();

        builder.Property(e => e.IngestedUtc)
            .IsRequired();

        // Embedding column type and HNSW index configured in VectorDbContext.OnModelCreating
    }
}
