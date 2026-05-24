using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary;

internal sealed partial class ItineraryBuilder(
    IEnumerable<IItinerarySection> sections,
    ILogger<ItineraryBuilder> logger) : IItineraryBuilder
{
    public async Task<ItineraryDto> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken)
    {
        var tasks = sections.Select(s => RunSafeAsync(s, prefs, cancellationToken)).ToArray();
        var results = await Task.WhenAll(tasks);
        var ordered = results
            .OrderBy(r => r.Order)
            .Select(r => new ItinerarySectionDto(r.Key, r.Data, r.Diagnostic))
            .ToList();
        // Id stamped by ItineraryService.UpsertPlanAsync before persistence — Plan.Id is the source of truth.
        return new ItineraryDto(Guid.Empty, DateTime.UtcNow, ordered);
    }

    // Sections fail-soft for operational/transient errors (LLM down, DB down, vector search timeout)
    // so one slow service doesn't kill the whole snapshot. Programmer-bug exceptions
    // (NullReferenceException, InvalidOperationException, StackOverflowException, etc.) DO bubble
    // so they surface during dev/CI and aren't silently masked.
    private async Task<ItinerarySectionResult> RunSafeAsync(
        IItinerarySection section,
        UserPreferencesSnapshot prefs,
        CancellationToken cancellationToken)
    {
        try
        {
            return await section.BuildAsync(prefs, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (LlmException ex)
        {
            return Diagnostic(section, ex);
        }
        catch (HttpRequestException ex)
        {
            return Diagnostic(section, ex);
        }
        catch (TimeoutException ex)
        {
            return Diagnostic(section, ex);
        }
        catch (DbUpdateException ex)
        {
            return Diagnostic(section, ex);
        }
        catch (JsonException ex)
        {
            return Diagnostic(section, ex);
        }
    }

    private ItinerarySectionResult Diagnostic(IItinerarySection section, Exception ex)
    {
        LogSectionFailed(logger, section.Key, ex);
        return new ItinerarySectionResult(
            section.Key,
            section.Order,
            JsonNode.Parse("{}")!,
            $"section-failed:{ex.GetType().Name}");
    }

    [LoggerMessage(EventId = 7001, Level = LogLevel.Warning, Message = "Itinerary section failed key={Key}")]
    private static partial void LogSectionFailed(ILogger logger, string key, Exception ex);
}
