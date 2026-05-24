using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.AiChat.Entities;

namespace Reshape.ElectricAi.AiChat.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Title).IsRequired().HasMaxLength(120);
        builder.Property(c => c.CreatedUtc).IsRequired();
        builder.Property(c => c.LastMessageUtc).IsRequired();
        builder.Property(c => c.UserMessageCount).IsRequired();
        builder.Property(c => c.IsGenerating).IsRequired();
        builder.Property(c => c.GeneratingStartedUtc);

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(c => new { c.UserId, c.LastMessageUtc })
            .IsDescending(false, true);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
