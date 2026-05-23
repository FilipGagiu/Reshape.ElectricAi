using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class FaqControllerTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private FaqApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new FaqApiFactory(postgres);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Ingest_ValidRequest_Returns204()
    {
        var request = new IngestQARequest(
            $"Test question {Guid.NewGuid()}",
            [new IngestAnswerRequest("Test answer.")]);

        var response = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Ingest_DuplicateQuestion_Returns204Idempotent()
    {
        var questionText = $"Duplicate question {Guid.NewGuid()}";
        var request = new IngestQARequest(questionText, [new IngestAnswerRequest("Answer.")]);

        var first = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);
        var second = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Ingest_EmptyQuestionText_Returns400()
    {
        var request = new IngestQARequest("", [new IngestAnswerRequest("Answer.")]);

        var response = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ingest_EmptyAnswersList_Returns400()
    {
        var request = new IngestQARequest($"Valid question {Guid.NewGuid()}", []);

        var response = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ingest_WithCategoryValues_Returns204()
    {
        var request = new IngestQARequest(
            $"Where can I park? {Guid.NewGuid()}",
            [new IngestAnswerRequest("Use lot B.")],
            QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
            {
                { Category.Transport, ["Car"] }
            });

        var response = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Search_ValidFilter_Returns200WithList()
    {
        var questionText = $"What time do gates open? {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(questionText, [new IngestAnswerRequest("Gates open at 14:00.")]),
            JsonOptions);

        var filter = new QuestionSearchFilter(questionText, TopK: 1);
        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<RetrievedQA>>(JsonOptions);
        results.Should().NotBeNull();
        results!.Should().NotBeEmpty();
        results![0].QuestionText.Should().Be(questionText);
    }

    [Fact]
    public async Task Search_NoUserContext_ReturnsAllCategories()
    {
        // Fake embedding is deterministic per text — cosine score is 1.0 only when query
        // text exactly matches an ingested question. Query each question's text separately
        // and confirm it returns regardless of its category tag.
        var transportQ = $"Transport question {Guid.NewGuid()}";
        var musicQ = $"Music question {Guid.NewGuid()}";

        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(transportQ, [new IngestAnswerRequest("Transport answer.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Transport, ["Car"] } }),
            JsonOptions);
        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(musicQ, [new IngestAnswerRequest("Music answer.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Music, ["Rock"] } }),
            JsonOptions);

        var transportResponse = await _client.PostAsJsonAsync("/api/v1/faq/search",
            new QuestionSearchFilter(transportQ, UserContext: null, TopK: 1), JsonOptions);
        var musicResponse = await _client.PostAsJsonAsync("/api/v1/faq/search",
            new QuestionSearchFilter(musicQ, UserContext: null, TopK: 1), JsonOptions);

        transportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        musicResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var transportResults = await transportResponse.Content.ReadFromJsonAsync<List<RetrievedQA>>(JsonOptions);
        var musicResults = await musicResponse.Content.ReadFromJsonAsync<List<RetrievedQA>>(JsonOptions);

        transportResults!.Select(r => r.QuestionText).Should().Contain(transportQ);
        musicResults!.Select(r => r.QuestionText).Should().Contain(musicQ);
    }

    [Fact]
    public async Task Search_WithUserContext_FiltersToMatchingCategory()
    {
        var tag = Guid.NewGuid().ToString("N");
        var transportQ = $"Car parking details {tag}";
        var foodQ = $"Food stall location {tag}";

        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(transportQ, [new IngestAnswerRequest("Lot B north side.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Transport, ["Car"] } }),
            JsonOptions);
        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(foodQ, [new IngestAnswerRequest("Near the east gate.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Food, ["Vegan"] } }),
            JsonOptions);

        var filter = new QuestionSearchFilter(
            tag,
            UserContext: new Dictionary<Category, IReadOnlyList<string>>
                { { Category.Transport, ["Car"] } },
            TopK: 10);
        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<RetrievedQA>>(JsonOptions);
        results.Should().NotBeNull();
        results!.Should().AllSatisfy(r =>
            r.QuestionText.Should().NotBe(foodQ));
    }

    [Fact]
    public async Task Search_EmptyQueryText_Returns400()
    {
        var filter = new QuestionSearchFilter("");

        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_TopKOutOfRange_Returns400()
    {
        var filter = new QuestionSearchFilter("some query", TopK: 0);

        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_NoResults_Returns200WithEmptyList()
    {
        var filter = new QuestionSearchFilter($"completely unrelated {Guid.NewGuid()}", TopK: 1);

        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<RetrievedQA>>(JsonOptions);
        results.Should().NotBeNull();
    }
}
