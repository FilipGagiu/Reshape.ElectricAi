using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence;

public class VectorDbContext(DbContextOptions<VectorDbContext> options, IOptions<ChatOptions> chatOptions)
    : DbContext(options)
{
    private readonly int _embeddingDimensions = chatOptions.Value.EmbeddingDimensions;

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<EventEntry> EventEntries => Set<EventEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("vector");
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VectorDbContext).Assembly);

        var vectorType = $"vector({_embeddingDimensions})";

        modelBuilder.Entity<DocumentChunk>()
            .Property(c => c.Embedding)
            .HasColumnType(vectorType);

        modelBuilder.Entity<DocumentChunk>()
            .HasIndex(c => c.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        modelBuilder.Entity<Question>()
            .Property(q => q.Embedding)
            .HasColumnType(vectorType);

        modelBuilder.Entity<Question>()
            .HasIndex(q => q.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        modelBuilder.Entity<Answer>()
            .Property(a => a.Embedding)
            .HasColumnType(vectorType);

        modelBuilder.Entity<Answer>()
            .HasIndex(a => a.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        modelBuilder.Entity<EventEntry>()
            .Property(e => e.Embedding)
            .HasColumnType(vectorType);

        modelBuilder.Entity<EventEntry>()
            .HasIndex(e => e.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}
