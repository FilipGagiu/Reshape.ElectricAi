using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class UserPreferenceArtistConfiguration : IEntityTypeConfiguration<UserPreferenceArtist>
{
    public void Configure(EntityTypeBuilder<UserPreferenceArtist> builder)
    {
        builder.ToTable("UserPreferenceArtists");
        builder.HasKey(x => new { x.UserId, x.ArtistName });

        builder.Property(x => x.ArtistName).HasMaxLength(200).IsRequired();
    }
}
