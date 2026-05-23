using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos;

public sealed record UserFeedPrefs(
    IReadOnlySet<string> Artists,
    IReadOnlySet<MusicGenre> Genres);
