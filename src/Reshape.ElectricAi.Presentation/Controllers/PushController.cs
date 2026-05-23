using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Dtos.Notifications;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class PushController(IPushService pushService) : ControllerBase
{
    [HttpGet("public-key")]
    [AllowAnonymous]
    public ActionResult<VapidPublicKeyResponse> GetPublicKey() =>
        Ok(new VapidPublicKeyResponse(pushService.GetVapidPublicKey()));

    [HttpPost("subscribe")]
    [AllowAnonymous]
    public async Task<ActionResult> SubscribeAsync(
        [FromBody] SubscribeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        await pushService.SubscribeAsync(request, userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("unsubscribe")]
    [AllowAnonymous]
    public async Task<ActionResult> UnsubscribeAsync(
        [FromBody] UnsubscribeRequest request,
        CancellationToken cancellationToken)
    {
        await pushService.UnsubscribeAsync(request.Endpoint, cancellationToken);
        return NoContent();
    }

    [HttpPost("send")]
    [AllowAnonymous]
    public async Task<ActionResult<SendResult>> SendAsync(
        [FromBody] SendRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new PushPayload(request.Title, request.Body, request.Icon, request.Badge, request.Url);
        var result = await pushService.SendToAllAsync(payload, cancellationToken);
        return Ok(result);
    }

    private Guid? ResolveUserId()
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var userId) ? userId : null;
    }
}
