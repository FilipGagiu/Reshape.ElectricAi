using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos;

public sealed record FeedEntryDto(
    Guid Id,
    string Title,
    string Body,
    Category PrimaryCategory,
    bool IsGeneral,
    IReadOnlyList<string> TargetArtists,
    IReadOnlyList<MusicGenre> TargetGenres,
    DateTime PublishedUtc,
    DateTime? UpdatedUtc);
