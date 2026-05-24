using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Conversation;

public sealed record StartConversationRequest(
    string Message,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null);
