using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class GroupPreferenceCuisineConfiguration : IEntityTypeConfiguration<GroupPreferenceCuisine>
{
    public void Configure(EntityTypeBuilder<GroupPreferenceCuisine> builder)
    {
        builder.ToTable("GroupPreferenceCuisines");
        builder.HasKey(x => new { x.GroupId, x.Cuisine });

        builder.Property(x => x.Cuisine).HasConversion<string>().HasMaxLength(30);
    }
}
