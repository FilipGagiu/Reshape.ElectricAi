using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class UserPreferenceFoodRestrictionConfiguration : IEntityTypeConfiguration<UserPreferenceFoodRestriction>
{
    public void Configure(EntityTypeBuilder<UserPreferenceFoodRestriction> builder)
    {
        builder.ToTable("UserPreferenceFoodRestrictions");
        builder.HasKey(x => new { x.UserId, x.Restriction });

        builder.Property(x => x.Restriction).HasConversion<string>().HasMaxLength(30);
    }
}
