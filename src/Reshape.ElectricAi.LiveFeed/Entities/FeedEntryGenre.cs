using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.LiveFeed.Entities;

public class FeedEntryGenre
{
    public Guid FeedEntryId { get; set; }
    public MusicGenre Genre { get; set; }

    public FeedEntry? FeedEntry { get; set; }
}
