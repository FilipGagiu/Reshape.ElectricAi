using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.AiChat.Entities;

public class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public ConversationActor Actor { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public int OrderIndex { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
