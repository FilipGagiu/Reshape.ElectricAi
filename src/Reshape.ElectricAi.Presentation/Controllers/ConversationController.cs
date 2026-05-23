using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Dtos.Conversation;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

/// <summary>
/// AI conversation endpoint. Retrieves relevant context from all vector stores and generates a grounded answer.
/// Anonymous — no JWT required.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class ConversationController(IConversationService conversationService) : ControllerBase
{
    /// <summary>Ask the Electric Castle AI assistant a question.</summary>
    /// <remarks>
    /// Performs parallel KNN search across documentation, FAQ, and event vector stores.
    /// Top results above the relevance threshold are fed as context to the language model.
    /// Returns a fallback message when no sufficiently relevant context is found.
    /// </remarks>
    /// <param name="request">The question to answer.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Answer generated (or fallback when context relevance is too low).</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConversationResponse>> AskAsync(
        [FromBody] ConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await conversationService.AskAsync(request, cancellationToken);
        return Ok(result);
    }
}
