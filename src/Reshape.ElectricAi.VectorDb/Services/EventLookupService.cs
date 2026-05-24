using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Services.Itinerary;
using Reshape.ElectricAi.VectorDb.Persistence;

namespace Reshape.ElectricAi.VectorDb.Services;

// Opens a short-lived VectorDbContext per call via the factory: TopArtistsSection runs
// FindByTitlesAsync and IVectorSearchService.SearchEventsAsync concurrently via Task.WhenAll,
// and ItineraryBuilder fans out three vector-touching sections concurrently — both would trip
// EF's ConcurrencyDetector if every call shared the same scoped context.
public sealed class EventLookupService(IDbContextFactory<VectorDbContext> contextFactory) : IEventLookupService
{
    private const char LikeEscape = '\\';

    public async Task<IReadOnlyList<MatchedEvent>> FindByTitlesAsync(
        IReadOnlyList<string> titles,
        CancellationToken cancellationToken)
    {
        if (titles is null || titles.Count == 0)
        {
            return [];
        }

        var patterns = titles
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => $"%{EscapeLike(t.Trim())}%")
            .ToArray();

        if (patterns.Length == 0)
        {
            return [];
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await context.EventEntries
            .AsNoTracking()
            .Where(e => patterns.Any(p => EF.Functions.ILike(e.Title, p, LikeEscape.ToString())))
            .Select(e => new { e.Id, e.Title, e.EventUtc })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new MatchedEvent(r.Id, r.Title, r.EventUtc)).ToList();
    }

    private static string EscapeLike(string input)
    {
        if (input.Length == 0) return input;
        var needsEscape = false;
        foreach (var c in input)
        {
            if (c == '%' || c == '_' || c == LikeEscape)
            {
                needsEscape = true;
                break;
            }
        }
        if (!needsEscape) return input;

        var sb = new System.Text.StringBuilder(input.Length + 8);
        foreach (var c in input)
        {
            if (c == '%' || c == '_' || c == LikeEscape)
            {
                sb.Append(LikeEscape);
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
