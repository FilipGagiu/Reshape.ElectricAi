using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Conversation;

public sealed record ConversationRequest(
    string QuestionText,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null);
