namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IEventLookupService
{
    Task<IReadOnlyList<MatchedEvent>> FindByTitlesAsync(IReadOnlyList<string> titles, CancellationToken cancellationToken);
}
