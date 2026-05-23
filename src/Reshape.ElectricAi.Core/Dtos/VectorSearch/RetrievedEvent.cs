namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record RetrievedEvent(
    Guid FeedEntryId,
    string Title,
    string TextRepresentation,
    DateTimeOffset EventUtc,
    float Score);
