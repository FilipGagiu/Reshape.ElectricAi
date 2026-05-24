using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.VectorDb.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

/// <summary>
/// One-time genre backfill for artist document chunks. Anonymous by design.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/genre-backfill")]
[Produces("application/json")]
public sealed class GenreBackfillController(GenreBackfillJobChannel jobChannel) : ControllerBase
{
    /// <summary>Queue a one-time backfill pass over artist document chunks that are missing category tags.</summary>
    /// <remarks>
    /// Anonymous on purpose — this is a one-time operational endpoint. Accepts the request and
    /// runs the backfill in a background worker decoupled from the HTTP connection. For each
    /// chunk whose content starts with <c>Artist:</c> and whose <c>category_tags</c> is empty,
    /// the worker asks the LLM to classify music genres from the <see cref="Reshape.ElectricAi.Core.Enums.MusicGenre"/>
    /// enum and writes the resulting tags using <c>CategoryTagsHelper.ToTags</c>. Idempotent —
    /// chunks with existing tags are skipped. Only one backfill may be in progress at a time;
    /// subsequent requests return 409 until the running pass finishes.
    /// </remarks>
    /// <response code="202">Backfill accepted and running in the background.</response>
    /// <response code="409">A genre backfill is already in progress.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Run()
    {
        if (!jobChannel.TryEnqueue())
            throw new ConflictException("genre-backfill-in-progress", "A genre backfill operation is already in progress.");

        return Accepted();
    }
}
