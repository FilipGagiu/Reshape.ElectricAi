using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans", t => t.HasCheckConstraint(
            "ck_plans_owner_xor_group",
            "(\"OwnerUserId\" IS NULL) <> (\"GroupId\" IS NULL)"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Scope).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.TicketType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.ContentJson).HasColumnType("jsonb").IsRequired();

        builder.Property(x => x.GeneratedUtc).IsRequired();

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasIndex(x => x.OwnerUserId);
        builder.HasIndex(x => x.GroupId);
    }
}
