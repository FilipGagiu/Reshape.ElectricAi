using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.VectorDb.Services;

internal static class GenreBackfillSchema
{
    internal const string SystemPrompt =
        "You are a music genre classifier for Electric Castle festival artists. " +
        "Given an artist's chunk content, choose 1 to 5 genre labels from the provided enum " +
        "that best describe the artist's musical style. " +
        "Use only the listed labels exactly as spelled. " +
        "Prefer specific labels over generic ones. " +
        "If unsure or the content has insufficient information, return ['Other'].";
}

internal sealed record ArtistGenreClassification(IReadOnlyList<MusicGenre> Genres);
