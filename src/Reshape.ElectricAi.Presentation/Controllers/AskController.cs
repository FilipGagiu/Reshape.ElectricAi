using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Dtos.Ask;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

/// <summary>
/// Anonymous one-shot Q&amp;A endpoint. Retrieves context from all vector stores and
/// generates a grounded answer. Renamed from <c>/api/v1/conversation</c> after the
/// multi-turn <c>/api/v1/conversations</c> slice landed.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/ask")]
[Produces("application/json")]
public sealed class AskController(IAskService askService) : ControllerBase
{
    /// <summary>Ask the Electric Castle AI assistant a question.</summary>
    /// <remarks>
    /// Performs KNN search across documentation, FAQ, and event vector stores.
    /// If <c>userContext</c> is provided on the request, only items whose category tags
    /// overlap the context are considered. Omit or pass <c>null</c> to search all
    /// categories. Top results above the relevance threshold are fed as context to the
    /// language model. Returns a fallback message when no sufficiently relevant context
    /// is found.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AskResponse>> AskAsync(
        [FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        var result = await askService.AskAsync(request, cancellationToken);
        return Ok(result);
    }
}
