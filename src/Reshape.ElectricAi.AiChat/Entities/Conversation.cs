namespace Reshape.ElectricAi.AiChat.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime LastMessageUtc { get; set; }
    public int UserMessageCount { get; set; }
    public bool IsGenerating { get; set; }
    public DateTime? GeneratingStartedUtc { get; set; }

    public List<ConversationMessage> Messages { get; set; } = [];
}
