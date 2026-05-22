using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class GroupPreferenceFoodRestrictionConfiguration : IEntityTypeConfiguration<GroupPreferenceFoodRestriction>
{
    public void Configure(EntityTypeBuilder<GroupPreferenceFoodRestriction> builder)
    {
        builder.ToTable("GroupPreferenceFoodRestrictions");
        builder.HasKey(x => new { x.GroupId, x.Restriction });

        builder.Property(x => x.Restriction).HasConversion<string>().HasMaxLength(30);
    }
}
