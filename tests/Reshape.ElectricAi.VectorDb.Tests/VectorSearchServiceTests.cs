using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.VectorDb.Entities;
using Reshape.ElectricAi.VectorDb.Persistence;
using Reshape.ElectricAi.VectorDb.Services;

namespace Reshape.ElectricAi.VectorDb.Tests;

[Collection("VectorDb")]
public sealed class VectorSearchServiceTests(VectorDbFixture fixture)
{
    private IngestService BuildIngestService(VectorDbContext context) =>
        new(
            new VectorRepository<Document>(context),
            new VectorRepository<Question>(context),
            new VectorRepository<EventEntry>(context),
            fixture.CreateEmbeddingService());

    private VectorSearchService BuildSearchService(VectorDbContext context) =>
        new(context, fixture.CreateEmbeddingService());

    [Fact]
    public async Task SearchDocumentsAsync_WithMatchingText_ReturnsChunk()
    {
        var uniqueText = $"The campsite opens at noon on Thursday {Guid.NewGuid()}";
        await using (var ingestCtx = fixture.CreateContext())
        {
            await BuildIngestService(ingestCtx).IngestDocumentAsync(
                new IngestDocumentRequest("Camping Guide", uniqueText));
        }

        await using var searchCtx = fixture.CreateContext();
        var results = await BuildSearchService(searchCtx).SearchDocumentsAsync(
            new DocumentSearchFilter(QueryText: uniqueText, TopK: 1));

        results.Should().NotBeEmpty();
        results[0].Content.Should().Contain("campsite");
    }

    [Fact]
    public async Task SearchDocumentsAsync_WithCategoryFilter_ExcludesNonMatchingCategory()
    {
        var matchText = $"VIP tent info {Guid.NewGuid()}";
        var noMatchText = $"General admission area {Guid.NewGuid()}";

        await using (var ingestCtx = fixture.CreateContext())
        {
            var svc = BuildIngestService(ingestCtx);
            await svc.IngestDocumentAsync(new IngestDocumentRequest(
                "VIP Doc", matchText,
                new Dictionary<Category, IReadOnlyList<string>> { { Category.Ticket, ["VIP"] } }));
            await svc.IngestDocumentAsync(new IngestDocumentRequest(
                "General Doc", noMatchText,
                new Dictionary<Category, IReadOnlyList<string>> { { Category.Ticket, ["General"] } }));
        }

        await using var searchCtx = fixture.CreateContext();
        var results = await BuildSearchService(searchCtx).SearchDocumentsAsync(
            new DocumentSearchFilter(
                QueryText: matchText,
                UserContext: new Dictionary<Category, IReadOnlyList<string>> { { Category.Ticket, ["VIP"] } },
                TopK: 10));

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.DocumentTitle.Should().Be("VIP Doc"));
    }

    [Fact]
    public async Task SearchQuestionsAsync_WithMatchingText_ReturnsQuestionWithAnswers()
    {
        var questionText = $"Where is the medical tent? {Guid.NewGuid()}";
        await using (var ingestCtx = fixture.CreateContext())
        {
            await BuildIngestService(ingestCtx).IngestQAAsync(
                new IngestQARequest(questionText, [new IngestAnswerRequest("Near the East entrance.")]));
        }

        await using var searchCtx = fixture.CreateContext();
        var results = await BuildSearchService(searchCtx).SearchQuestionsAsync(
            new QuestionSearchFilter(QueryText: questionText, TopK: 1));

        results.Should().NotBeEmpty();
        results[0].Answers.Should().ContainSingle(a => a.AnswerText == "Near the East entrance.");
    }

    [Fact]
    public async Task SearchQuestionsAsync_WithCategoryFilter_ExcludesNonMatchingCategory()
    {
        var musicQ = $"Who headlines the music stage? {Guid.NewGuid()}";
        var foodQ = $"What food stalls are available? {Guid.NewGuid()}";

        await using (var ingestCtx = fixture.CreateContext())
        {
            var svc = BuildIngestService(ingestCtx);
            await svc.IngestQAAsync(new IngestQARequest(musicQ,
                [new IngestAnswerRequest("Top artists perform each night.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>> { { Category.Music, ["Lineup"] } }));
            await svc.IngestQAAsync(new IngestQARequest(foodQ,
                [new IngestAnswerRequest("Various international food options.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>> { { Category.Food, ["International"] } }));
        }

        await using var searchCtx = fixture.CreateContext();
        var results = await BuildSearchService(searchCtx).SearchQuestionsAsync(
            new QuestionSearchFilter(
                QueryText: musicQ,
                UserContext: new Dictionary<Category, IReadOnlyList<string>> { { Category.Music, ["Lineup"] } },
                TopK: 10));

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.QuestionText.Should().Contain("music stage"));
    }

    [Fact]
    public async Task SearchEventsAsync_WithMatchingText_ReturnsEvent()
    {
        var feedEntryId = Guid.NewGuid();
        var description = $"Arctic Monkeys on Main Stage at 21:00 {Guid.NewGuid()}";
        await using (var ingestCtx = fixture.CreateContext())
        {
            await BuildIngestService(ingestCtx).IngestEventAsync(
                new IngestEventRequest(feedEntryId, "Arctic Monkeys", description, DateTimeOffset.UtcNow.AddDays(5)));
        }

        await using var searchCtx = fixture.CreateContext();
        var results = await BuildSearchService(searchCtx).SearchEventsAsync(
            new EventSearchFilter(QueryText: description, TopK: 1));

        results.Should().NotBeEmpty();
        results[0].Title.Should().Be("Arctic Monkeys");
        results[0].FeedEntryId.Should().Be(feedEntryId);
    }

    [Fact]
    public async Task SearchEventsAsync_WithCategoryFilter_ExcludesNonMatchingCategory()
    {
        var musicEvent = $"Drum and Bass night {Guid.NewGuid()}";
        var weatherEvent = $"Storm warning issued {Guid.NewGuid()}";

        await using (var ingestCtx = fixture.CreateContext())
        {
            var svc = BuildIngestService(ingestCtx);
            await svc.IngestEventAsync(new IngestEventRequest(
                Guid.NewGuid(), "DnB Night", musicEvent, DateTimeOffset.UtcNow.AddDays(1),
                new Dictionary<Category, IReadOnlyList<string>> { { Category.Music, ["DnB"] } }));
            await svc.IngestEventAsync(new IngestEventRequest(
                Guid.NewGuid(), "Weather Alert", weatherEvent, DateTimeOffset.UtcNow.AddDays(1),
                new Dictionary<Category, IReadOnlyList<string>> { { Category.Weather, ["Storm"] } }));
        }

        await using var searchCtx = fixture.CreateContext();
        var results = await BuildSearchService(searchCtx).SearchEventsAsync(
            new EventSearchFilter(
                QueryText: musicEvent,
                UserContext: new Dictionary<Category, IReadOnlyList<string>> { { Category.Music, ["DnB"] } },
                TopK: 10));

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Title.Should().Be("DnB Night"));
    }
}
