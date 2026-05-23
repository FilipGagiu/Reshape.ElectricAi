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

/// <summary>
/// Live organizer-to-attendee push channel. Organizers publish feed entries (delays,
/// weather alerts, stage moves) and connected attendees receive them in real time
/// over Server-Sent Events. Entries can be broadcast to everyone (<c>IsGeneral=true</c>)
/// or targeted to users whose preferences overlap the entry's
/// <see cref="PublishFeedEntryRequest.TargetArtists"/> /
/// <see cref="PublishFeedEntryRequest.TargetGenres"/>.
/// </summary>
/// <remarks>
/// Authentication model:
/// <list type="bullet">
///   <item>CRUD endpoints require a JWT bearer token. Publish/Update/Delete additionally require role <c>Organizer</c>.</item>
///   <item>The SSE <c>/stream</c> endpoint is intentionally anonymous in v1 (EventSource cannot send Authorization headers; the secure query-string-token middleware is deferred). It accepts a <c>?userId={guid}</c> query parameter that drives the targeting preferences lookup.</item>
/// </list>
/// </remarks>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class FeedController(
    IFeedService feed,
    IFeedBroadcaster broadcaster,
    IUserPrefsProvider prefsProvider) : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>List recent feed entries personalized for the current user.</summary>
    /// <remarks>
    /// Returns the most recent 100 not-deleted entries, ordered by <c>PublishedUtc DESC</c>,
    /// filtered to those whose targeting matches the caller's preferences
    /// (<see cref="IUserPrefsProvider"/>). General entries (<c>IsGeneral=true</c>) are always
    /// included. Optionally narrow by <paramref name="category"/>.
    /// </remarks>
    /// <param name="category">Optional <see cref="Category"/> filter. Omit for all categories.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Personalized feed list.</response>
    /// <response code="401">Missing or invalid bearer token.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<FeedEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<FeedEntryDto>>> ListRecentEntriesForCurrentUserAsync(
        [FromQuery] Category? category, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(User);
        var prefs = await prefsProvider.GetPrefsByUserIdAsync(userId, cancellationToken);
        var entries = await feed.ListRecentEntriesMatchingPrefsAsync(prefs, category, 100, cancellationToken);
        return Ok(entries);
    }

    /// <summary>Publish a new feed entry as an organizer.</summary>
    /// <remarks>
    /// Creates the entry, persists it to the <c>feed</c> schema, then broadcasts a
    /// <c>feed.created</c> event to every connected subscriber whose preferences match
    /// the entry's targeting. Broadcast happens AFTER <c>SaveChangesAsync</c> returns so
    /// a rollback never leaks an envelope.
    ///
    /// Validation rules (FluentValidation):
    /// <list type="bullet">
    ///   <item><c>Title</c> 1..200 chars, <c>Body</c> 1..4000 chars.</item>
    ///   <item><c>TargetArtists</c> ≤ 25 entries (case-insensitive unique), each 1..100 chars.</item>
    ///   <item><c>TargetGenres</c> ≤ 12 entries (unique, valid enum).</item>
    ///   <item>If <c>IsGeneral=false</c> at least one target artist OR genre is required (error code <c>no-targeting-and-not-general</c>).</item>
    /// </list>
    /// </remarks>
    /// <param name="request">Entry to publish.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="201">Entry created. Returns the persisted DTO. <c>Location</c> header points at the entry's URL.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Missing or invalid bearer token.</response>
    /// <response code="403">Authenticated but not in role <c>Organizer</c>.</response>
    [HttpPost]
    [Authorize(Roles = "Organizer")]
    [ProducesResponseType(typeof(FeedEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

    /// <summary>Update an existing feed entry as an organizer.</summary>
    /// <remarks>
    /// Replaces all editable fields on the entry, sets <c>UpdatedUtc</c>, and broadcasts a
    /// <c>feed.updated</c> event after the database transaction commits.
    /// </remarks>
    /// <param name="id">Entry id (path).</param>
    /// <param name="request">New field values.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Updated entry DTO.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Missing or invalid bearer token.</response>
    /// <response code="403">Authenticated but not in role <c>Organizer</c>.</response>
    /// <response code="404">Entry not found.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Organizer")]
    [ProducesResponseType(typeof(FeedEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>Delete a feed entry as an organizer.</summary>
    /// <remarks>
    /// Hard-deletes the entry row (and cascades to <c>FeedEntryArtist</c>/<c>FeedEntryGenre</c>),
    /// best-effort removes the matching <c>vector.event_entries</c> row, then broadcasts a
    /// <c>feed.deleted</c> event so connected subscribers can drop it from their rendered list.
    /// Idempotent: deleting a missing entry is a no-op (no broadcast, no exception). When the
    /// vector-row removal throws, the FeedEntry is still gone and the broadcast still ships --
    /// a warning is logged with the stale <c>FeedEntryId</c>.
    /// </remarks>
    /// <param name="id">Entry id (path).</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="204">Deletion accepted (or already-deleted — idempotent).</response>
    /// <response code="401">Missing or invalid bearer token.</response>
    /// <response code="403">Authenticated but not in role <c>Organizer</c>.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteEntryByIdAsOrganizerAsync(
        [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        // See UpdateEntryByIdAsOrganizerAsync above re. discarded sub-claim assertion.
        _ = GetCurrentUserId(User);
        await feed.DeleteEntryByIdAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Open a Server-Sent Events stream of feed events targeted to a user.</summary>
    /// <remarks>
    /// Long-lived HTTP response with <c>Content-Type: text/event-stream</c>. The server emits:
    /// <list type="bullet">
    ///   <item><c>event: feed.created | feed.updated | feed.deleted</c> followed by <c>id:</c> + <c>data:</c> (JSON-serialized <see cref="FeedEntryDto"/>) on every matching publish/update/delete.</item>
    ///   <item><c>: keepalive</c> comment line every 25 seconds (heartbeat to defeat proxy idle timeouts).</item>
    /// </list>
    /// On (re)connect:
    /// <list type="bullet">
    ///   <item>If the client sends a <c>Last-Event-ID</c> request header with the last delivered event id, the server replays up to 10 entries published after that cursor (filtered by current preferences). Malformed cursor falls back to the most recent 10.</item>
    ///   <item>If no <c>Last-Event-ID</c>, the server replays the most recent 10 matching entries.</item>
    /// </list>
    /// Identity is taken from the <c>?userId={guid}</c> query parameter purely for targeting prefs lookup
    /// (acknowledged v1 limitation -- the EventSource browser API cannot send an Authorization header
    /// and the secure query-string-token middleware is deferred). When the parameter is omitted, the
    /// caller receives only general (broadcast-to-all) entries.
    /// </remarks>
    /// <param name="userId">Optional. Drives the targeting prefs lookup for this connection. Omit for general-only.</param>
    /// <param name="cancellationToken">Cancellation token. SSE connections live until the client disconnects.</param>
    /// <response code="200">Stream open. Body is <c>text/event-stream</c>, not JSON.</response>
    [HttpGet("stream")]
    [AllowAnonymous]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
