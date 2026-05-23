using Pgvector;

namespace Reshape.ElectricAi.VectorDb.Entities;

public class Question
{
    public Guid Id { get; set; }
    public required string Text { get; set; }
    public required string TextHash { get; set; }
    public required Vector Embedding { get; set; }
    public string[] CategoryTags { get; set; } = [];
    public DateTimeOffset IngestedUtc { get; set; }
    public ICollection<Answer> Answers { get; set; } = [];
}
