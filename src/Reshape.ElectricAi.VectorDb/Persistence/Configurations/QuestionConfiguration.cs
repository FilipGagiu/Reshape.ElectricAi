using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text)
            .IsRequired();

        builder.Property(q => q.TextHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(q => q.TextHash)
            .IsUnique();

        builder.Property(q => q.CategoryTags)
            .HasColumnType("text[]");

        builder.HasIndex(q => q.CategoryTags)
            .HasMethod("gin");

        builder.Property(q => q.IngestedUtc)
            .IsRequired();

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Embedding column type and HNSW index configured in VectorDbContext.OnModelCreating
    }
}
