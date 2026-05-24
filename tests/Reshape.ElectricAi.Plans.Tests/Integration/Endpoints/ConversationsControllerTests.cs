using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.AiChat.Entities;
using Reshape.ElectricAi.AiChat.Persistence;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Dtos.Conversation;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class ConversationsControllerTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string ValidPassword = "ValidPass1!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private ConversationsApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new ConversationsApiFactory(postgres);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Start_ValidMessage_Returns201WithGuidAndTitle()
    {
        var token = await RegisterAndGetTokenAsync("start-ok");
        var body = new StartConversationRequest("Hello there, what time does the festival open?");

        var response = await SendAsync(HttpMethod.Post, "/api/v1/conversations", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<StartConversationResponse>(JsonOptions);
        dto.Should().NotBeNull();
        dto!.Id.Should().NotBe(Guid.Empty);
        dto.Title.Should().StartWith("Hello there");
        dto.Reply.Actor.Should().Be(ConversationActor.Bot);
        dto.Reply.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Start_MessageTooLong_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("start-too-long");
        var body = new StartConversationRequest(new string('x', 1001));

        var response = await SendAsync(HttpMethod.Post, "/api/v1/conversations", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Start_EmptyMessage_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("start-empty");
        var body = new StartConversationRequest("");

        var response = await SendAsync(HttpMethod.Post, "/api/v1/conversations", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Start_Unauthenticated_Returns401()
    {
        var body = new StartConversationRequest("Hello");

        var response = await _client.PostAsJsonAsync("/api/v1/conversations", body, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Continue_AppendsToHistory_AndIncludesPriorTurnsInPrompt()
    {
        var token = await RegisterAndGetTokenAsync("continue-history");

        var first = await SendAsync(HttpMethod.Post, "/api/v1/conversations",
            new StartConversationRequest("First message"), token);
        first.EnsureSuccessStatusCode();
        var firstDto = (await first.Content.ReadFromJsonAsync<StartConversationResponse>(JsonOptions))!;

        var second = await SendAsync(HttpMethod.Post, $"/api/v1/conversations/{firstDto.Id}",
            new ContinueConversationRequest("Second message"), token);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondDto = (await second.Content.ReadFromJsonAsync<ContinueConversationResponse>(JsonOptions))!;
        secondDto.Reply.Actor.Should().Be(ConversationActor.Bot);

        // The fake captured both prompts. The second call should include the first user message and
        // first bot reply in the message list before the new user message.
        var second_calls = _factory.OpenAi.Captured;
        second_calls.Should().HaveCount(2);
        var lastCall = second_calls[1];
        var userTurns = lastCall.Where(m => m.Role == Core.Services.LlmChatRole.User).ToList();
        userTurns.Should().HaveCount(2);
        userTurns[0].Content.Should().Contain("First message");
        userTurns[1].Content.Should().Contain("Second message");
        lastCall.Should().Contain(m => m.Role == Core.Services.LlmChatRole.Assistant);

        var detail = await SendAsync(HttpMethod.Get, $"/api/v1/conversations/{firstDto.Id}", null, token);
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailDto = (await detail.Content.ReadFromJsonAsync<ConversationDetailDto>(JsonOptions))!;
        detailDto.Replies.Should().HaveCount(4);
        detailDto.Replies[0].Actor.Should().Be(ConversationActor.User);
        detailDto.Replies[1].Actor.Should().Be(ConversationActor.Bot);
        detailDto.Replies[2].Actor.Should().Be(ConversationActor.User);
        detailDto.Replies[3].Actor.Should().Be(ConversationActor.Bot);
    }

    [Fact]
    public async Task Get_NonOwner_Returns404()
    {
        var ownerToken = await RegisterAndGetTokenAsync("get-owner");
        var startResp = await SendAsync(HttpMethod.Post, "/api/v1/conversations",
            new StartConversationRequest("private message"), ownerToken);
        var startDto = (await startResp.Content.ReadFromJsonAsync<StartConversationResponse>(JsonOptions))!;

        var otherToken = await RegisterAndGetTokenAsync("get-other");
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/conversations/{startDto.Id}", null, otherToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Continue_NonOwner_Returns404()
    {
        var ownerToken = await RegisterAndGetTokenAsync("cont-owner");
        var startResp = await SendAsync(HttpMethod.Post, "/api/v1/conversations",
            new StartConversationRequest("Initial"), ownerToken);
        var startDto = (await startResp.Content.ReadFromJsonAsync<StartConversationResponse>(JsonOptions))!;

        var otherToken = await RegisterAndGetTokenAsync("cont-other");
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/conversations/{startDto.Id}",
            new ContinueConversationRequest("Trying to hijack"), otherToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Continue_AfterCapReached_Returns409Full()
    {
        var token = await RegisterAndGetTokenAsync("cap");
        var startResp = await SendAsync(HttpMethod.Post, "/api/v1/conversations",
            new StartConversationRequest("first"), token);
        var startDto = (await startResp.Content.ReadFromJsonAsync<StartConversationResponse>(JsonOptions))!;

        await SetUserMessageCountAsync(startDto.Id, 20);

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/conversations/{startDto.Id}",
            new ContinueConversationRequest("over the line"), token);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions);
        envelope!.Error.Code.Should().Be("conversation-full");
    }

    [Fact]
    public async Task Continue_StaleLock_AcquiredBySecondRequest()
    {
        var token = await RegisterAndGetTokenAsync("stale-lock");
        var startResp = await SendAsync(HttpMethod.Post, "/api/v1/conversations",
            new StartConversationRequest("initial"), token);
        var startDto = (await startResp.Content.ReadFromJsonAsync<StartConversationResponse>(JsonOptions))!;

        // Force a stale lock 10 minutes in the past (older than the 60s lock timeout).
        await ForceStaleLockAsync(startDto.Id, DateTime.UtcNow.AddMinutes(-10));

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/conversations/{startDto.Id}",
            new ContinueConversationRequest("after stale lock"), token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Continue_FreshLockHeld_Returns409Busy()
    {
        var token = await RegisterAndGetTokenAsync("busy-lock");
        var startResp = await SendAsync(HttpMethod.Post, "/api/v1/conversations",
            new StartConversationRequest("initial"), token);
        var startDto = (await startResp.Content.ReadFromJsonAsync<StartConversationResponse>(JsonOptions))!;

        // Simulate the lock currently held (e.g. another request mid-flight).
        await ForceStaleLockAsync(startDto.Id, DateTime.UtcNow);

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/conversations/{startDto.Id}",
            new ContinueConversationRequest("racing"), token);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions);
        envelope!.Error.Code.Should().Be("conversation-busy");
    }

    [Fact]
    public async Task Continue_LlmFailure_LeavesLockReleasedAndAllowsRetry()
    {
        var token = await RegisterAndGetTokenAsync("llm-fail");
        var startResp = await SendAsync(HttpMethod.Post, "/api/v1/conversations",
            new StartConversationRequest("initial"), token);
        var startDto = (await startResp.Content.ReadFromJsonAsync<StartConversationResponse>(JsonOptions))!;

        _factory.OpenAi.ThrowOnNextCall = true;
        var firstAttempt = await SendAsync(HttpMethod.Post, $"/api/v1/conversations/{startDto.Id}",
            new ContinueConversationRequest("triggers failure"), token);
        firstAttempt.IsSuccessStatusCode.Should().BeFalse();

        // Lock should be released after the failure; the next request must succeed.
        var retry = await SendAsync(HttpMethod.Post, $"/api/v1/conversations/{startDto.Id}",
            new ContinueConversationRequest("retry"), token);
        retry.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task List_ReturnsOnlyCallerConversations_OrderedByLastMessageDesc()
    {
        var token = await RegisterAndGetTokenAsync("list-owner");
        var otherToken = await RegisterAndGetTokenAsync("list-other");

        var firstId = await StartAsync(token, "alpha message");
        // Force LastMessageUtc to a known earlier time so ordering is deterministic.
        await SetLastMessageUtcAsync(firstId, DateTime.UtcNow.AddMinutes(-10));
        var secondId = await StartAsync(token, "beta message");
        await StartAsync(otherToken, "noise from another user");

        var list = await SendAsync(HttpMethod.Get, "/api/v1/conversations", null, token);
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await list.Content.ReadFromJsonAsync<List<ConversationSummaryDto>>(JsonOptions);
        items.Should().NotBeNull();
        var orderedIds = items!.Select(c => c.Id).ToList();
        orderedIds.Should().BeEquivalentTo(new[] { firstId, secondId });
        orderedIds[0].Should().Be(secondId);
    }

    private sealed record ErrorEnvelope(ErrorPayload Error);
    private sealed record ErrorPayload(string Code, string Message);

    private async Task<Guid> StartAsync(string token, string message)
    {
        var resp = await SendAsync(HttpMethod.Post, "/api/v1/conversations",
            new StartConversationRequest(message), token);
        resp.EnsureSuccessStatusCode();
        var dto = (await resp.Content.ReadFromJsonAsync<StartConversationResponse>(JsonOptions))!;
        return dto.Id;
    }

    private async Task SetUserMessageCountAsync(Guid conversationId, int newCount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        await db.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UserMessageCount, newCount));
    }

    private async Task ForceStaleLockAsync(Guid conversationId, DateTime startedUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        await db.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsGenerating, true)
                .SetProperty(c => c.GeneratingStartedUtc, (DateTime?)startedUtc));
    }

    private async Task SetLastMessageUtcAsync(Guid conversationId, DateTime ts)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        await db.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.LastMessageUtc, ts));
    }

    private async Task<string> RegisterAndGetTokenAsync(string slug)
    {
        var email = $"conv-{slug}-{Guid.NewGuid():N}@example.com";
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, ValidPassword), JsonOptions);
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
        return auth.AccessToken;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, string? token)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }
        return await _client.SendAsync(request);
    }
}
