using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Dtos;
using Reshape.ElectricAi.LiveFeed.Dtos.Mapping;

namespace Reshape.ElectricAi.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class FeedController(
    IFeedService feed,
    IFeedBroadcaster broadcaster,
    IUserPrefsProvider prefsProvider) : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<FeedEntryDto>>> ListRecentEntriesForCurrentUserAsync(
        [FromQuery] Category? category, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(User);
        var prefs = await prefsProvider.GetPrefsByUserIdAsync(userId, cancellationToken);
        var entries = await feed.ListRecentEntriesMatchingPrefsAsync(prefs, category, 100, cancellationToken);
        return Ok(entries);
    }

    [HttpPost]
    [Authorize(Roles = "Organizer")]
    public async Task<ActionResult<FeedEntryDto>> PublishEntryAsOrganizerAsync(
        [FromBody] PublishFeedEntryRequest request, CancellationToken cancellationToken)
    {
        var organizerId = GetCurrentUserId(User);
        var dto = await feed.PublishEntryAsync(organizerId, request.ToCommand(), cancellationToken);
        // 201 + Location header per CODE.md ## Controllers. CreatedAtAction(nameof(...))
        // would attempt route-name resolution which fails when MVC strips the Async
        // suffix from the action name -- nameof() returns the full name and no route
        // matches. Build the Location URL directly instead.
        return Created($"/api/v1/feed/{dto.Id}", dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Organizer")]
    public async Task<ActionResult<FeedEntryDto>> UpdateEntryByIdAsOrganizerAsync(
        [FromRoute] Guid id, [FromBody] UpdateFeedEntryRequest request, CancellationToken cancellationToken)
    {
        // Defensive sub-claim assertion. [Authorize(Roles="Organizer")] gates the role,
        // but GetCurrentUserId throws UnauthorizedException if `sub` is missing/malformed.
        // Discard result -- service signature doesn't take organizerId today; if audit
        // logging arrives the userId will be passed through.
        _ = GetCurrentUserId(User);
        var dto = await feed.UpdateEntryByIdAsync(id, request.ToCommand(), cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Organizer")]
    public async Task<IActionResult> SoftDeleteEntryByIdAsOrganizerAsync(
        [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        // See UpdateEntryByIdAsOrganizerAsync above re. discarded sub-claim assertion.
        _ = GetCurrentUserId(User);
        await feed.SoftDeleteEntryByIdAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("stream")]
    [AllowAnonymous]
    [Produces("text/event-stream")]
    public async Task StreamFeedToCurrentUserAsync(
        [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        WriteSseResponseHeaders();

        var effectiveUserId = userId ?? Guid.Empty;
        var prefs = effectiveUserId == Guid.Empty
            ? new UserFeedPrefs(new HashSet<string>(), new HashSet<MusicGenre>())
            : await prefsProvider.GetPrefsByUserIdAsync(effectiveUserId, cancellationToken);

        var lastEventId = Request.Headers["Last-Event-ID"].FirstOrDefault();

        using var writeLock = new SemaphoreSlim(1, 1);
        var heartbeatTask = RunHeartbeatLoopAsync(writeLock, cancellationToken);
        try
        {
            await foreach (var env in broadcaster.SubscribeUserToStreamAsync(effectiveUserId, prefs, lastEventId, cancellationToken))
                await WriteSseEventFrameAsync(env, writeLock, cancellationToken);
        }
        // Client disconnect cancels the request -- normal SSE lifecycle, not an error.
        // Without these catches the exception escapes to ExceptionHandlerMiddleware
        // which clears the response and emits a JSON error envelope, replacing the
        // text/event-stream content type the test (and any real client) expects.
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (IOException) { }
        finally
        {
            try { await heartbeatTask; }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (IOException) { }
        }
    }

    private void WriteSseResponseHeaders()
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";
    }

    private async Task WriteSseEventFrameAsync(
        FeedEventEnvelope env, SemaphoreSlim writeLock, CancellationToken ct)
    {
        await writeLock.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(env.Entry, _jsonOpts);
            var kindWire = env.Kind switch
            {
                FeedEventKind.Created => "created",
                FeedEventKind.Updated => "updated",
                FeedEventKind.Deleted => "deleted",
                _ => throw new InvalidOperationException("Unknown FeedEventKind")
            };
            await Response.WriteAsync($"event: feed.{kindWire}\n", ct);
            await Response.WriteAsync($"id: {env.EventId}\n", ct);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        finally { writeLock.Release(); }
    }

    private async Task RunHeartbeatLoopAsync(SemaphoreSlim writeLock, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(25));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await writeLock.WaitAsync(ct);
                try
                {
                    await Response.WriteAsync(": keepalive\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                }
                finally { writeLock.Release(); }
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (IOException) { }
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id)
            ? id
            : throw new UnauthorizedException("missing-sub-claim", "Subject claim missing or invalid");
    }
}
