using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Configurations;

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .IsRequired();

        builder.Property(c => c.CategoryTags)
            .HasColumnType("text[]");

        builder.HasIndex(c => c.CategoryTags)
            .HasMethod("gin");

        builder.Property(c => c.ChunkIndex)
            .IsRequired();

        // Embedding column type and HNSW index configured in VectorDbContext.OnModelCreating
        // because dimensions are config-driven (ChatOptions.EmbeddingDimensions)
    }
}
