namespace Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;

public sealed record ArtistDayDto(DateOnly Date, IReadOnlyList<RecommendedArtistDto> Artists);
