using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Groups;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public sealed class GroupsController(IGroupService groupService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<GroupDto>> CreateAsync(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var dto = await groupService.CreateAsync(userId, request, cancellationToken);
        return Created($"/api/v1/groups/{dto.Id}", dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GroupDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var dto = await groupService.GetAsync(id, userId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<GroupMemberDto>> AddMemberAsync(
        Guid id,
        [FromBody] AddGroupMemberRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var member = await groupService.AddMemberAsync(id, userId, request, cancellationToken);
        return Created($"/api/v1/groups/{id}", member);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var callerUserId = ResolveUserId();
        await groupService.RemoveMemberAsync(id, callerUserId, userId, cancellationToken);
        return NoContent();
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
