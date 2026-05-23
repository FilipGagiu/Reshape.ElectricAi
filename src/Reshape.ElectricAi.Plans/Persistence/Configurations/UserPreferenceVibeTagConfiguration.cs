using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public sealed class UserPreferenceVibeTagConfiguration : IEntityTypeConfiguration<UserPreferenceVibeTag>
{
    public void Configure(EntityTypeBuilder<UserPreferenceVibeTag> builder)
    {
        builder.ToTable("UserPreferenceVibeTags");
        builder.HasKey(x => new { x.UserId, x.Value });
        builder.Property(x => x.Value).HasMaxLength(60).IsRequired();

        builder.HasOne(x => x.UserPreferences)
            .WithMany(p => p.VibeTags)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
