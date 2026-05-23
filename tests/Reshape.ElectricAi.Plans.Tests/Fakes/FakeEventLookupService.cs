using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Tests.Fakes;

public sealed class FakeEventLookupService : IEventLookupService
{
    public List<IReadOnlyList<string>> Calls { get; } = [];
    public IReadOnlyList<MatchedEvent> Results { get; set; } = [];

    public Task<IReadOnlyList<MatchedEvent>> FindByTitlesAsync(IReadOnlyList<string> titles, CancellationToken cancellationToken)
    {
        Calls.Add(titles);
        return Task.FromResult(Results);
    }
}
