using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos;

public sealed record FeedEventEnvelope(
    FeedEventKind Kind,
    string EventId,
    FeedEntryDto Entry);
