namespace Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;

public sealed record RecommendedArtistDto(Guid Id, string Title, DateTimeOffset EventUtc, float Score);
