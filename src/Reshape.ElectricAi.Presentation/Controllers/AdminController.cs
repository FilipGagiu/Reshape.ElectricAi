using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
public sealed class AdminController(EcDataSeeder seeder) : ControllerBase
{
    /// <summary>Trigger ingestion of all data files from the given absolute directory path.</summary>
    /// <remarks>
    /// Reads lineup.json, faqs-ec-website.json, and all ec-pages/*.md files from
    /// <c>dataPath</c> and ingests them into the vector store. Idempotent — already-ingested
    /// content is skipped by hash. Intended for production bootstrapping via a secure API call.
    /// </remarks>
    /// <param name="request">Absolute path to the data directory on the server filesystem.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="204">Seeding completed successfully.</response>
    /// <response code="400">Validation failed (e.g. empty path).</response>
    /// <response code="401">Missing or invalid JWT token.</response>
    /// <response code="403">Authenticated user does not have the Organizer role.</response>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SeedAsync(
        [FromBody] SeedDataRequest request, CancellationToken cancellationToken)
    {
        await seeder.SeedAsync(request.DataPath, cancellationToken);
        return NoContent();
    }
}
