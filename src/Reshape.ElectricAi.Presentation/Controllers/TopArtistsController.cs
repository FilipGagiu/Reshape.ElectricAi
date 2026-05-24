using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/top-artists")]
public sealed class TopArtistsController(ITopArtistsService topArtistsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TopArtistsResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var artists = await topArtistsService.GetTopForUserAsync(userId, cancellationToken);
        return Ok(new TopArtistsResponse(artists));
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
