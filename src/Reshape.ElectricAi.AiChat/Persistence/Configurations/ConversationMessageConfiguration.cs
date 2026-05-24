using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.AiChat.Entities;

namespace Reshape.ElectricAi.AiChat.Persistence.Configurations;

internal sealed class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("conversation_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ConversationId).IsRequired();
        builder.Property(m => m.Actor)
            .IsRequired()
            .HasConversion<byte>();
        builder.Property(m => m.Content).IsRequired().HasMaxLength(2000);
        builder.Property(m => m.CreatedUtc).IsRequired();
        builder.Property(m => m.OrderIndex).IsRequired();

        builder.HasIndex(m => new { m.ConversationId, m.OrderIndex }).IsUnique();
    }
}
