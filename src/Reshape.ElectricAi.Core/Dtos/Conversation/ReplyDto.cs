using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Conversation;

public sealed record ReplyDto(string Message, ConversationActor Actor, DateTime CreatedUtc);
