namespace Reshape.ElectricAi.Core.Dtos.Conversation;

public sealed record ConversationSummaryDto(
    Guid Id,
    string Title,
    DateTime CreatedUtc,
    DateTime LastMessageUtc,
    int UserMessageCount);
