using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public sealed class ItineraryController(IItineraryService service) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<ActionResult<ItineraryResponse>> GenerateAsync(
        [FromBody] ItineraryGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var response = await service.GenerateAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<ItineraryResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var response = await service.GetAsync(userId, cancellationToken);
        if (response is null)
        {
            throw new NotFoundException("itinerary-not-found", "No itinerary generated yet.");
        }
        return Ok(response);
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
