using Reshape.ElectricAi.Core.Dtos.Itinerary;

namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IItineraryService
{
    Task<ItineraryResponse> GenerateAsync(Guid userId, ItineraryGenerationRequest request, CancellationToken cancellationToken);
    Task<ItineraryResponse?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<ItineraryResponse> RebuildAfterPrefsChangeAsync(Guid userId, CancellationToken cancellationToken);
}
