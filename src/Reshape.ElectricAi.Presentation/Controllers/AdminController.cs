using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.VectorDb.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

/// <summary>
/// Admin operations for seeding production data. Requires Organizer role.
/// </summary>
[ApiController]
[Authorize(Roles = "Organizer")]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class AdminController(SeedJobChannel seedJobChannel) : ControllerBase
{
    /// <summary>Queue ingestion of all data files from the given absolute directory path.</summary>
    /// <remarks>
    /// Accepts the request and runs ingestion in a background worker decoupled from the HTTP
    /// connection. Reads lineup.json, faqs-ec-website.json, and all ec-pages/*.md files from
    /// <c>dataPath</c> and ingests them into the vector store. Idempotent — already-ingested
    /// content is skipped by hash. Only one seed may be in progress at a time; subsequent
    /// requests return 409 until the running seed finishes.
    /// </remarks>
    /// <param name="request">Absolute path to the data directory on the server filesystem.</param>
    /// <param name="cancellationToken">Request cancellation token (unused — the seed runs detached from this request).</param>
    /// <response code="202">Seeding accepted and running in the background.</response>
    /// <response code="400">Validation failed (e.g. empty path).</response>
    /// <response code="401">Missing or invalid JWT token.</response>
    /// <response code="403">Authenticated user does not have the Organizer role.</response>
    /// <response code="409">A seed is already in progress.</response>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Seed(
        [FromBody] SeedDataRequest request, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!seedJobChannel.TryEnqueue(request.DataPath))
            throw new ConflictException("seed-in-progress", "A seed operation is already in progress.");

        return Accepted();
    }
}
