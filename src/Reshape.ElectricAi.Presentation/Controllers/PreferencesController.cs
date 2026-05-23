using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public sealed class PreferencesController(IPreferencesService preferencesService) : ControllerBase
{
    private readonly IPreferencesService _preferencesService = preferencesService;

    [HttpGet]
    public async Task<ActionResult<PreferencesDto>> GetAsync(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var dto = await _preferencesService.GetAsync(userId, cancellationToken);
        return Ok(dto);
    }

    [HttpPut]
    public async Task<ActionResult<PreferencesDto>> ReplaceAsync(
        [FromBody] PreferencesReplaceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var dto = await _preferencesService.ReplaceAsync(userId, request, cancellationToken);
        return Ok(dto);
    }

    [HttpPatch]
    public async Task<ActionResult<PreferencesDto>> PatchAsync(
        [FromBody] PreferencesPatchRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var dto = await _preferencesService.PatchAsync(userId, request, cancellationToken);
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
