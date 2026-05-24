using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.VectorDb.Persistence;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed partial class TopArtistsService(
    VectorDbContext context,
    IUserPrefsProvider prefsProvider) : ITopArtistsService
{
    private const int ReturnCount = 5;
    private const string ArtistPrefix = "Artist:";
    private const string GenresPrefix = "Genres:";

    public async Task<IReadOnlyList<string>> GetTopForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var prefs = await prefsProvider.GetPrefsByUserIdAsync(userId, cancellationToken);
        if (prefs.Genres.Count == 0)
        {
            return [];
        }

        var displayNames = prefs.Genres
            .Select(g => ToDisplayName(g.ToString()))
            .ToArray();

        var contents = await context.DocumentChunks
            .AsNoTracking()
            .Where(c => c.Content.StartsWith(ArtistPrefix))
            .Select(c => c.Content)
            .ToListAsync(cancellationToken);

        return contents
            .Where(c => MatchesAnyGenre(c, displayNames))
            .Select(ExtractArtistName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(ReturnCount)
            .ToList();
    }

    private static string ToDisplayName(string pascalCase) =>
        CamelBoundary().Replace(pascalCase, " ");

    private static string? ExtractArtistName(string content)
    {
        var start = ArtistPrefix.Length;
        if (start >= content.Length)
        {
            return null;
        }

        var end = content.IndexOf('\n', start);
        var slice = end > start ? content[start..end] : content[start..];
        return slice.Trim();
    }

    private static bool MatchesAnyGenre(string content, string[] displayNames)
    {
        var genreLine = FindGenresLine(content);
        if (genreLine is null)
        {
            return false;
        }

        foreach (var name in displayNames)
        {
            if (genreLine.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string? FindGenresLine(string content)
    {
        var idx = content.IndexOf(GenresPrefix, StringComparison.Ordinal);
        if (idx < 0)
        {
            return null;
        }

        var lineStart = idx + GenresPrefix.Length;
        var lineEnd = content.IndexOf('\n', lineStart);
        return lineEnd > lineStart ? content[lineStart..lineEnd] : content[lineStart..];
    }

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])")]
    private static partial Regex CamelBoundary();
}
