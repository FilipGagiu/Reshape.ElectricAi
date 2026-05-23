using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reshape.ElectricAi.Core.Dtos.Conversation;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
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
}
