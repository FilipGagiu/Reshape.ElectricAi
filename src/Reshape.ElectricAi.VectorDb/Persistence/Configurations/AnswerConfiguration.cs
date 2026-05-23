using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Configurations;

internal sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("answers");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Text)
            .IsRequired();

        builder.Property(a => a.CategoryTags)
            .HasColumnType("text[]");

        builder.HasIndex(a => a.CategoryTags)
            .HasMethod("gin");

        builder.Property(a => a.IngestedUtc)
            .IsRequired();

        // Embedding column type and HNSW index configured in VectorDbContext.OnModelCreating
    }
}
