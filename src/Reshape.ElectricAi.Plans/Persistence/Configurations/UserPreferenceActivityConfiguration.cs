using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class UserPreferenceActivityConfiguration : IEntityTypeConfiguration<UserPreferenceActivity>
{
    public void Configure(EntityTypeBuilder<UserPreferenceActivity> builder)
    {
        builder.ToTable("UserPreferenceActivities");
        builder.HasKey(x => new { x.UserId, x.Activity });

        builder.Property(x => x.Activity).HasConversion<string>().HasMaxLength(20);
    }
}
