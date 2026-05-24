using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Conversation;

public sealed record ContinueConversationRequest(
    string Message,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null);
