using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

public class GroupPreferenceArtistConfiguration : IEntityTypeConfiguration<GroupPreferenceArtist>
{
    public void Configure(EntityTypeBuilder<GroupPreferenceArtist> builder)
    {
        builder.ToTable("GroupPreferenceArtists");
        builder.HasKey(x => new { x.GroupId, x.ArtistName });

        builder.Property(x => x.ArtistName).HasMaxLength(200).IsRequired();
    }
}
