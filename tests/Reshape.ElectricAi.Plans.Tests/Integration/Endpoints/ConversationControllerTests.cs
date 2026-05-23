using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos.Conversation;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class ConversationControllerTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private ConversationApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new ConversationApiFactory(postgres);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Ask_EmptyQuestionText_Returns400()
    {
        var request = new ConversationRequest("");

        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ask_QuestionTextExceedsMaxLength_Returns400()
    {
        var request = new ConversationRequest(new string('x', 501));

        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ask_ValidQuestionWithMatchingContext_Returns200WithAiAnswer()
    {
        var questionText = $"What time do gates open? {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(questionText, [new IngestAnswerRequest("Gates open at 14:00.")]),
            JsonOptions);

        var request = new ConversationRequest(questionText);
        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConversationResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Answer.Should().Be(ConversationFakeOpenAiClient.FakeAnswer);
    }

    [Fact]
    public async Task Ask_ValidQuestionWithNoRelevantContext_Returns200WithFallback()
    {
        var uniqueQuery = $"zzz-unrelated-{Guid.NewGuid():N}-zzz";

        var request = new ConversationRequest(uniqueQuery);
        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConversationResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Answer.Should().Contain("Chads and Stacies");
    }

    [Fact]
    public async Task Ask_WithMatchingUserContext_Returns200WithAiAnswer()
    {
        var questionText = $"What time does the rock stage open? {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(questionText, [new IngestAnswerRequest("Rock stage opens at 16:00.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Music, ["Rock"] } }),
            JsonOptions);

        var request = new ConversationRequest(
            questionText,
            UserContext: new Dictionary<Category, IReadOnlyList<string>>
                { { Category.Music, ["Rock"] } });
        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConversationResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Answer.Should().Be(ConversationFakeOpenAiClient.FakeAnswer);
    }

    [Fact]
    public async Task Ask_WithMismatchedUserContext_ReturnsFallback()
    {
        var questionText = $"Where is the food court? {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(questionText, [new IngestAnswerRequest("North gate.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Food, ["Vegan"] } }),
            JsonOptions);

        var request = new ConversationRequest(
            questionText,
            UserContext: new Dictionary<Category, IReadOnlyList<string>>
                { { Category.Transport, ["Car"] } });
        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConversationResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Answer.Should().Contain("Chads and Stacies");
    }

    [Fact]
    public async Task Ask_WithNullUserContext_StillFindsContext()
    {
        var questionText = $"How do I get to the festival? {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(questionText, [new IngestAnswerRequest("Shuttle from Cluj every hour.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Transport, ["Shuttle"] } }),
            JsonOptions);

        var request = new ConversationRequest(questionText, UserContext: null);
        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConversationResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Answer.Should().Be(ConversationFakeOpenAiClient.FakeAnswer);
    }

    [Fact]
    public async Task Ask_WithMatchingDocumentUserContext_Returns200WithAiAnswer()
    {
        var docTitle = $"Stage map {Guid.NewGuid()}";
        var docContent = $"Rock stage is on the north meadow tag-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var ingest = scope.ServiceProvider.GetRequiredService<IIngestService>();
            await ingest.IngestDocumentAsync(
                new IngestDocumentRequest(docTitle, docContent,
                    CategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                        { { Category.Music, ["Rock"] } }),
                CancellationToken.None);
        }

        var request = new ConversationRequest(
            docContent,
            UserContext: new Dictionary<Category, IReadOnlyList<string>>
                { { Category.Music, ["Rock"] } });
        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConversationResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Answer.Should().Be(ConversationFakeOpenAiClient.FakeAnswer);
    }

    [Fact]
    public async Task Ask_WithMismatchedDocumentUserContext_ReturnsFallback()
    {
        var docTitle = $"Vegan stalls {Guid.NewGuid()}";
        var docContent = $"Vegan stalls cluster near east gate tag-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var ingest = scope.ServiceProvider.GetRequiredService<IIngestService>();
            await ingest.IngestDocumentAsync(
                new IngestDocumentRequest(docTitle, docContent,
                    CategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                        { { Category.Food, ["Vegan"] } }),
                CancellationToken.None);
        }

        var request = new ConversationRequest(
            docContent,
            UserContext: new Dictionary<Category, IReadOnlyList<string>>
                { { Category.Transport, ["Car"] } });
        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConversationResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Answer.Should().Contain("Chads and Stacies");
    }

    [Fact]
    public async Task Ask_WithMatchingEventUserContext_Returns200WithAiAnswer()
    {
        var feedEntryId = Guid.NewGuid();
        var title = $"Rock stage delay {Guid.NewGuid()}";
        var textRep = $"Main rock stage delayed 30 min tag-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var ingest = scope.ServiceProvider.GetRequiredService<IIngestService>();
            await ingest.IngestEventAsync(
                new IngestEventRequest(feedEntryId, title, textRep, DateTimeOffset.UtcNow,
                    CategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                        { { Category.Music, ["Rock"] } }),
                CancellationToken.None);
        }

        var request = new ConversationRequest(
            textRep,
            UserContext: new Dictionary<Category, IReadOnlyList<string>>
                { { Category.Music, ["Rock"] } });
        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConversationResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Answer.Should().Be(ConversationFakeOpenAiClient.FakeAnswer);
    }

    [Fact]
    public async Task Ask_WithMismatchedEventUserContext_ReturnsFallback()
    {
        var feedEntryId = Guid.NewGuid();
        var title = $"Vegan food alert {Guid.NewGuid()}";
        var textRep = $"Extra vegan stall opening at 16:00 tag-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var ingest = scope.ServiceProvider.GetRequiredService<IIngestService>();
            await ingest.IngestEventAsync(
                new IngestEventRequest(feedEntryId, title, textRep, DateTimeOffset.UtcNow,
                    CategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                        { { Category.Food, ["Vegan"] } }),
                CancellationToken.None);
        }

        var request = new ConversationRequest(
            textRep,
            UserContext: new Dictionary<Category, IReadOnlyList<string>>
                { { Category.Transport, ["Car"] } });
        var response = await _client.PostAsJsonAsync("/api/v1/conversation", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConversationResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Answer.Should().Contain("Chads and Stacies");
    }
}
