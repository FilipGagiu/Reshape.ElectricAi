using Reshape.ElectricAi.Core.Dtos.Conversation;

namespace Reshape.ElectricAi.Core.Services;

public interface IConversationService
{
    Task<ConversationResponse> AskAsync(ConversationRequest request, CancellationToken cancellationToken = default);
}
