using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Services.Itinerary;
using Reshape.ElectricAi.Plans.Services.Itinerary;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services.Itinerary;

public class ItineraryBuilderTests
{
    private static UserPreferencesSnapshot Empty() => new(
        Guid.NewGuid(), null, null, null, null,
        [], [], [], [], [], [], null, null, null, null, null, null);

    [Fact]
    public async Task Runs_all_sections_parallel_and_orders_result()
    {
        var sem = new SemaphoreSlim(0, 2);
        var sections = new IItinerarySection[]
        {
            new DelaySection("b", 20, sem),
            new DelaySection("a", 10, sem),
        };
        var builder = new ItineraryBuilder(sections, NullLogger<ItineraryBuilder>.Instance);

        var task = builder.BuildAsync(Empty(), CancellationToken.None);
        await Task.Delay(50);
        sem.Release(2);
        var dto = await task;

        Assert.Equal(["a", "b"], dto.Sections.Select(s => s.Key).ToArray());
    }

    [Fact]
    public async Task Failing_section_emits_diagnostic_others_succeed()
    {
        var sections = new IItinerarySection[]
        {
            new ThrowingSection("bad", 50),
            new ConstSection("good", 10, JsonNode.Parse("""{"ok":true}""")!),
        };
        var builder = new ItineraryBuilder(sections, NullLogger<ItineraryBuilder>.Instance);
        var dto = await builder.BuildAsync(Empty(), CancellationToken.None);

        Assert.Collection(dto.Sections,
            s => { Assert.Equal("good", s.Key); Assert.Null(s.Diagnostic); },
            s => { Assert.Equal("bad", s.Key); Assert.Equal("section-failed:TimeoutException", s.Diagnostic); });
    }

    [Fact]
    public async Task Cancellation_propagates()
    {
        var sections = new IItinerarySection[] { new CancellableSection() };
        var builder = new ItineraryBuilder(sections, NullLogger<ItineraryBuilder>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => builder.BuildAsync(Empty(), cts.Token));
    }

    private sealed class DelaySection(string key, int order, SemaphoreSlim sem) : IItinerarySection
    {
        public string Key { get; } = key;
        public int Order { get; } = order;
        public async Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot _, CancellationToken ct)
        {
            await sem.WaitAsync(ct);
            return new ItinerarySectionResult(Key, Order, JsonNode.Parse("{}")!, null);
        }
    }

    private sealed class ThrowingSection(string key, int order) : IItinerarySection
    {
        public string Key { get; } = key;
        public int Order { get; } = order;
        public Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot _, CancellationToken __)
            => throw new TimeoutException("boom");
    }

    private sealed class ConstSection(string key, int order, JsonNode data) : IItinerarySection
    {
        public string Key { get; } = key;
        public int Order { get; } = order;
        private readonly JsonNode _data = data;
        public Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot _, CancellationToken __)
            => Task.FromResult(new ItinerarySectionResult(Key, Order, _data, null));
    }

    private sealed class CancellableSection : IItinerarySection
    {
        public string Key => "c";
        public int Order => 1;
        public async Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot _, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(Timeout.Infinite, ct);
            return null!;
        }
    }
}
