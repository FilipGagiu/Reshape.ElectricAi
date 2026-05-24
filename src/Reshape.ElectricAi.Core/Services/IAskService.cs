using Reshape.ElectricAi.Core.Dtos.Ask;

namespace Reshape.ElectricAi.Core.Services;

public interface IAskService
{
    Task<AskResponse> AskAsync(AskRequest request, CancellationToken cancellationToken = default);
}
