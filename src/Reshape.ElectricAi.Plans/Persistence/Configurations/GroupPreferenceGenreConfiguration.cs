using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class GroupPreferenceGenreConfiguration : IEntityTypeConfiguration<GroupPreferenceGenre>
{
    public void Configure(EntityTypeBuilder<GroupPreferenceGenre> builder)
    {
        builder.ToTable("GroupPreferenceGenres");
        builder.HasKey(x => new { x.GroupId, x.Genre });

        builder.Property(x => x.Genre).HasConversion<string>().HasMaxLength(30);
    }
}
