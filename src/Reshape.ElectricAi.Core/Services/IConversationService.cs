using Reshape.ElectricAi.Core.Dtos.Conversation;

namespace Reshape.ElectricAi.Core.Services;

public interface IConversationService
{
    Task<IReadOnlyList<ConversationSummaryDto>> ListAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<ConversationDetailDto> GetAsync(
        Guid userId, Guid conversationId, CancellationToken cancellationToken = default);

    Task<StartConversationResponse> StartAsync(
        Guid userId, StartConversationRequest request, CancellationToken cancellationToken = default);

    Task<ContinueConversationResponse> ContinueAsync(
        Guid userId, Guid conversationId, ContinueConversationRequest request,
        CancellationToken cancellationToken = default);
}
