using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.TicketType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Accommodation).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Transport).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AgeGroup).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CrewKind).HasConversion<string>().HasMaxLength(20);

        builder.Property(x => x.Name).HasMaxLength(80);
        builder.Property(x => x.Origin).HasMaxLength(120);
        builder.Property(x => x.AccommodationNote).HasMaxLength(200);
        builder.Property(x => x.TransportNote).HasMaxLength(200);
        builder.Property(x => x.CrewEstimatedSize);

        builder.Property(x => x.UpdatedUtc).IsRequired();

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasMany(x => x.Genres)
            .WithOne(x => x.UserPreferences!)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.FoodRestrictions)
            .WithOne(x => x.UserPreferences!)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Activities)
            .WithOne(x => x.UserPreferences!)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Artists)
            .WithOne(x => x.UserPreferences!)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Cuisines)
            .WithOne(x => x.UserPreferences!)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.VibeTags)
            .WithOne(x => x.UserPreferences!)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
