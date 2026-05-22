using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class GroupPreferenceActivityConfiguration : IEntityTypeConfiguration<GroupPreferenceActivity>
{
    public void Configure(EntityTypeBuilder<GroupPreferenceActivity> builder)
    {
        builder.ToTable("GroupPreferenceActivities");
        builder.HasKey(x => new { x.GroupId, x.Activity });

        builder.Property(x => x.Activity).HasConversion<string>().HasMaxLength(20);
    }
}
