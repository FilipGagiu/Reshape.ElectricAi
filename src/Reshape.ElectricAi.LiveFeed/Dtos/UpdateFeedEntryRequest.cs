using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.LiveFeed.Dtos;

public sealed record UpdateFeedEntryRequest(
    string Title,
    string Body,
    Category PrimaryCategory,
    bool IsGeneral,
    IReadOnlyList<string> TargetArtists,
    IReadOnlyList<MusicGenre> TargetGenres);
