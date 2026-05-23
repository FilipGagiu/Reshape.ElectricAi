namespace Reshape.ElectricAi.LiveFeed.Entities;

public class FeedEntryArtist
{
    public Guid FeedEntryId { get; set; }
    public string ArtistName { get; set; } = "";

    public FeedEntry? FeedEntry { get; set; }
}
