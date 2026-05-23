using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record IngestEventRequest(
    Guid FeedEntryId,
    string Title,
    string TextRepresentation,
    DateTimeOffset EventUtc,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? CategoryValues = null);
