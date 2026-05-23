using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("PushSubscriptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Endpoint).IsRequired();
        builder.HasIndex(x => x.Endpoint).IsUnique();

        builder.Property(x => x.P256dh).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Auth).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.Property(x => x.LastSeenUtc).IsRequired();

        builder.HasIndex(x => x.UserId);
    }
}
