using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public sealed class PreferencesController(
    IPreferencesService preferencesService,
    IItineraryService itineraryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PreferencesDto>> GetAsync(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var dto = await preferencesService.GetAsync(userId, cancellationToken);
        return Ok(dto);
    }

    [HttpPut]
    public async Task<ActionResult<PreferencesDto>> ReplaceAsync(
        [FromBody] PreferencesReplaceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var dto = await preferencesService.ReplaceAsync(userId, request, cancellationToken);
        // Snapshot rebuild runs AFTER the pref save has already committed. If it throws
        // (rate-limit 429, vector service down, etc.) the pref change is still persisted; the
        // caller sees a non-2xx mapped from the thrown DomainException, the snapshot stays
        // on the previous version, and the user can re-trigger via /itinerary/generate.
        await itineraryService.RebuildAfterPrefsChangeAsync(userId, cancellationToken);
        return Ok(dto);
    }

    [HttpPatch]
    public async Task<ActionResult<PreferencesDto>> PatchAsync(
        [FromBody] PreferencesPatchRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var dto = await preferencesService.PatchAsync(userId, request, cancellationToken);
        await itineraryService.RebuildAfterPrefsChangeAsync(userId, cancellationToken);
        return Ok(dto);
    }

    private Guid ResolveUserId()
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idClaim, out var userId))
        {
            throw new UnauthorizedException("invalid-token", "Access token is missing a valid subject.");
        }
        return userId;
    }
}
