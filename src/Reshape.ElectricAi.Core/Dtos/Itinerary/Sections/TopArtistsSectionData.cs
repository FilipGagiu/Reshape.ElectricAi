namespace Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;

public sealed record TopArtistsSectionData(
    IReadOnlyList<RecommendedArtistDto> TopOverall,
    IReadOnlyList<ArtistDayDto> ByDay);
