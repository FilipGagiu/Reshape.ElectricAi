using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class UserPreferenceCuisineConfiguration : IEntityTypeConfiguration<UserPreferenceCuisine>
{
    public void Configure(EntityTypeBuilder<UserPreferenceCuisine> builder)
    {
        builder.ToTable("UserPreferenceCuisines");
        builder.HasKey(x => new { x.UserId, x.Cuisine });

        builder.Property(x => x.Cuisine).HasConversion<string>().HasMaxLength(30);
    }
}
