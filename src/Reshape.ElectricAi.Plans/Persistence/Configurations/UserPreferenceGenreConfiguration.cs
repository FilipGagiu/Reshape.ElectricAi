using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class UserPreferenceGenreConfiguration : IEntityTypeConfiguration<UserPreferenceGenre>
{
    public void Configure(EntityTypeBuilder<UserPreferenceGenre> builder)
    {
        builder.ToTable("UserPreferenceGenres");
        builder.HasKey(x => new { x.UserId, x.Genre });

        builder.Property(x => x.Genre).HasConversion<string>().HasMaxLength(30);
    }
}
