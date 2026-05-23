using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.LiveFeed.Entities;

public class FeedEntry
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public Category PrimaryCategory { get; set; }
    public bool IsGeneral { get; set; }
    public Guid PublishedByUserId { get; set; }
    public DateTime PublishedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }

    public List<FeedEntryArtist> TargetArtists { get; set; } = [];
    public List<FeedEntryGenre> TargetGenres { get; set; } = [];
}
