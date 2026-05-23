using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos;

public sealed record PublishFeedEntryCommand(
    string Title,
    string Body,
    Category PrimaryCategory,
    bool IsGeneral,
    IReadOnlyList<string> TargetArtists,
    IReadOnlyList<MusicGenre> TargetGenres);
