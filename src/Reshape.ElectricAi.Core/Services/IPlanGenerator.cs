using Reshape.ElectricAi.Core.Dtos.Plans;

namespace Reshape.ElectricAi.Core.Services;

public interface IPlanGenerator
{
    Task<PlanGenerationResult> GenerateAsync(
        Guid userId,
        PlanGenerationRequest request,
        CancellationToken cancellationToken);
}
