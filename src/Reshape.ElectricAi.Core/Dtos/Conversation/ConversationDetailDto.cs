namespace Reshape.ElectricAi.Core.Dtos.Conversation;

public sealed record ConversationDetailDto(
    Guid Id,
    string Title,
    DateTime CreatedUtc,
    IReadOnlyList<ReplyDto> Replies);
