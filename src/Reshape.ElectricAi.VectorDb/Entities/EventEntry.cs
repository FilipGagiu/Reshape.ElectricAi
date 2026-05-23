using Pgvector;

namespace Reshape.ElectricAi.VectorDb.Entities;

public class EventEntry
{
    public Guid Id { get; set; }
    public Guid FeedEntryId { get; set; }
    public required string Title { get; set; }
    public required string TextRepresentation { get; set; }
    public required Vector Embedding { get; set; }
    public string[] CategoryTags { get; set; } = [];
    public DateTimeOffset EventUtc { get; set; }
    public DateTimeOffset IngestedUtc { get; set; }
}
