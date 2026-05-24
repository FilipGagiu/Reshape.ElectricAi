using Reshape.ElectricAi.Core.Dtos.Conversation;

namespace Reshape.ElectricAi.Core.Services;

public interface IHotQuestionsService
{
    Task<IReadOnlyList<HotQuestionDto>> GetTopAsync(int count, CancellationToken cancellationToken);
}
