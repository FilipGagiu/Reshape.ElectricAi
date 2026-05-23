using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

/// <summary>
/// Public FAQ search and ingestion. Both endpoints are anonymous — no JWT required.
/// Ingest is idempotent: posting the same question text twice is a no-op (duplicate skipped by hash).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class FaqController(
    IIngestService ingestService,
    IVectorSearchService vectorSearchService) : ControllerBase
{
    /// <summary>Ingest a new FAQ question and its answers, with optional category tags.</summary>
    /// <remarks>
    /// Idempotent: posting the same <c>questionText</c> (case-sensitive, whitespace-normalized)
    /// a second time is silently ignored — no duplicate is stored.
    ///
    /// Category tags on questions drive KNN filtering: a question tagged
    /// <c>{"Transport": ["Car"]}</c> is returned only when the caller's <c>userContext</c>
    /// includes <c>Transport.Car</c>. Omit <c>questionCategoryValues</c> to make the entry
    /// always visible regardless of caller context.
    /// </remarks>
    /// <param name="request">Question, answers, and optional category values.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="204">Ingested (or already exists — idempotent).</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestAsync(
        [FromBody] IngestQARequest request, CancellationToken cancellationToken)
    {
        await ingestService.IngestQAAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Find the <c>topK</c> most semantically similar FAQ questions to a query.</summary>
    /// <remarks>
    /// Embeds <c>queryText</c> and runs a cosine KNN search over stored question embeddings.
    /// If <c>userContext</c> is provided, only questions whose category tags overlap the context
    /// are considered. Omit or pass <c>null</c> to search all questions.
    ///
    /// Returns an empty list when no matching questions are found — never 404.
    /// </remarks>
    /// <param name="filter">Query text, optional user-context category map, and result count.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Ranked list of matching Q&amp;A pairs (may be empty).</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost("search")]
    [ProducesResponseType(typeof(IReadOnlyList<RetrievedQA>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<RetrievedQA>>> SearchAsync(
        [FromBody] QuestionSearchFilter filter, CancellationToken cancellationToken)
    {
        var results = await vectorSearchService.SearchQuestionsAsync(filter, cancellationToken);
        return Ok(results);
    }
}
