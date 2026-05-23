using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Plans;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class PlansController(IPlanGenerator planGenerator) : ControllerBase
{
    [HttpPost("generate")]
    [Authorize]
    public async Task<ActionResult<PlanGenerationResult>> GenerateAsync(
        [FromBody] PlanGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var result = await planGenerator.GenerateAsync(userId, request, cancellationToken);
        return Ok(result);
    }

    private Guid ResolveUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
        {
            throw new UnauthorizedException("invalid-token", "Subject claim missing or malformed.");
        }
        return userId;
    }
}
