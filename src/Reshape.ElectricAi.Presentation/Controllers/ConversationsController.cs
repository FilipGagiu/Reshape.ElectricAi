using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Conversation;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

/// <summary>
/// Persistent multi-turn conversations between authenticated users and the Electric Castle AI assistant.
/// Each conversation has at most <c>Conversation:UserMessageCap</c> user turns (default 20). A per-row
/// lock prevents concurrent generation requests for the same conversation.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/conversations")]
[Produces("application/json")]
public sealed class ConversationsController(
    IConversationService conversations,
    IHotQuestionsService hotQuestions) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryDto>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var items = await conversations.ListAsync(CurrentUserId(), cancellationToken);
        return Ok(items);
    }

    [HttpGet("hot-questions")]
    [ProducesResponseType(typeof(IReadOnlyList<HotQuestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HotQuestionDto>>> GetHotQuestionsAsync(
        CancellationToken cancellationToken)
    {
        var items = await hotQuestions.GetTopAsync(5, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ConversationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDetailDto>> GetAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var detail = await conversations.GetAsync(CurrentUserId(), id, cancellationToken);
        return Ok(detail);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StartConversationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StartConversationResponse>> StartAsync(
        [FromBody] StartConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await conversations.StartAsync(CurrentUserId(), request, cancellationToken);
        // CreatedAtAction(nameof(GetAsync)) fails because MVC strips the Async suffix
        // from action names during route resolution. Build the Location URL directly.
        return Created($"/api/v1/conversations/{result.Id}", result);
    }

    [HttpPost("{id:guid}")]
    [ProducesResponseType(typeof(ContinueConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContinueConversationResponse>> ContinueAsync(
        Guid id, [FromBody] ContinueConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await conversations.ContinueAsync(CurrentUserId(), id, request, cancellationToken);
        return Ok(result);
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new UnauthorizedException("missing-sub", "Token missing subject.");
        if (!Guid.TryParse(sub, out var userId))
        {
            throw new UnauthorizedException("missing-sub", "Token subject is not a valid Guid.");
        }
        return userId;
    }
}
