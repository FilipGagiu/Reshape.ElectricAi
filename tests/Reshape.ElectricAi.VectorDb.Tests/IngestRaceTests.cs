using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.VectorDb.Entities;
using Reshape.ElectricAi.VectorDb.Persistence;
using Reshape.ElectricAi.VectorDb.Services;

namespace Reshape.ElectricAi.VectorDb.Tests;

[Collection("VectorDb")]
public sealed class IngestRaceTests(VectorDbFixture fixture)
{
    [Fact]
    public async Task IngestDocumentAsync_WhenSameContentRaces_BothCallsSucceedAndOneRowInserted()
    {
        var content = $"Shared content body {Guid.NewGuid()}";
        var title = "Race Doc";

        var gate = new RaceGate(expected: 2);

        await using var ctxA = fixture.CreateContext();
        await using var ctxB = fixture.CreateContext();

        var serviceA = BuildIngestService(ctxA, gate);
        var serviceB = BuildIngestService(ctxB, gate);

        var taskA = Task.Run(() => serviceA.IngestDocumentAsync(new IngestDocumentRequest(title, content)));
        var taskB = Task.Run(() => serviceB.IngestDocumentAsync(new IngestDocumentRequest(title, content)));

        var act = async () => await Task.WhenAll(taskA, taskB);
        await act.Should().NotThrowAsync();

        await using var verifyCtx = fixture.CreateContext();
        var count = await verifyCtx.Documents
            .Where(d => d.Title == title)
            .Where(d => verifyCtx.DocumentChunks.Any(c => c.DocumentId == d.Id && c.Content.Contains(content)))
            .CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task IngestQAAsync_WhenSameTextRaces_BothCallsSucceedAndOneRowInserted()
    {
        var questionText = $"Where is the medical tent during the race? {Guid.NewGuid()}";

        var gate = new RaceGate(expected: 2);

        await using var ctxA = fixture.CreateContext();
        await using var ctxB = fixture.CreateContext();

        var serviceA = BuildIngestService(ctxA, gate);
        var serviceB = BuildIngestService(ctxB, gate);

        var payload = new IngestQARequest(
            questionText,
            [new IngestAnswerRequest("Near the East entrance.")]);

        var taskA = Task.Run(() => serviceA.IngestQAAsync(payload));
        var taskB = Task.Run(() => serviceB.IngestQAAsync(payload));

        var act = async () => await Task.WhenAll(taskA, taskB);
        await act.Should().NotThrowAsync();

        await using var verifyCtx = fixture.CreateContext();
        var count = await verifyCtx.Questions
            .Where(q => q.Text == questionText)
            .CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task IngestEventAsync_WhenSameFeedEntryIdRaces_BothCallsSucceedAndOneRowInserted()
    {
        var feedEntryId = Guid.NewGuid();
        var description = $"Headliner on Main Stage {Guid.NewGuid()}";

        var gate = new RaceGate(expected: 2);

        await using var ctxA = fixture.CreateContext();
        await using var ctxB = fixture.CreateContext();

        var serviceA = BuildIngestService(ctxA, gate);
        var serviceB = BuildIngestService(ctxB, gate);

        var payload = new IngestEventRequest(
            feedEntryId, "Headliner", description, DateTimeOffset.UtcNow.AddDays(3));

        var taskA = Task.Run(() => serviceA.IngestEventAsync(payload));
        var taskB = Task.Run(() => serviceB.IngestEventAsync(payload));

        var act = async () => await Task.WhenAll(taskA, taskB);
        await act.Should().NotThrowAsync();

        await using var verifyCtx = fixture.CreateContext();
        var count = await verifyCtx.EventEntries
            .Where(e => e.FeedEntryId == feedEntryId)
            .CountAsync();
        count.Should().Be(1);
    }

    private IngestService BuildIngestService(VectorDbContext context, RaceGate gate) =>
        new(
            new VectorRepository<Document>(context),
            new VectorRepository<Question>(context),
            new VectorRepository<EventEntry>(context),
            new GatedEmbeddingService(fixture.CreateEmbeddingService(), gate));
}

internal sealed class RaceGate
{
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly int _expected;
    private int _arrived;

    public RaceGate(int expected) => _expected = expected;

    public async Task ArriveAndWaitAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _arrived) >= _expected)
            _release.TrySetResult();
        await _release.Task.WaitAsync(cancellationToken);
    }
}

internal sealed class GatedEmbeddingService(IEmbeddingService inner, RaceGate gate) : IEmbeddingService
{
    private readonly AsyncLocal<bool> _gatedOnce = new();

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        await MaybeGateAsync(cancellationToken);
        return await inner.GenerateEmbeddingAsync(text, cancellationToken);
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        await MaybeGateAsync(cancellationToken);
        return await inner.GenerateEmbeddingsAsync(texts, cancellationToken);
    }

    private async Task MaybeGateAsync(CancellationToken cancellationToken)
    {
        if (_gatedOnce.Value) return;
        _gatedOnce.Value = true;
        await gate.ArriveAndWaitAsync(cancellationToken);
    }
}
