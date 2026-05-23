using Pgvector;

namespace Reshape.ElectricAi.VectorDb.Entities;

public class Answer
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public required string Text { get; set; }
    public required Vector Embedding { get; set; }
    public string[] CategoryTags { get; set; } = [];
    public DateTimeOffset IngestedUtc { get; set; }
    public Question Question { get; set; } = null!;
}
