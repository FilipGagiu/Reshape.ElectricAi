namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItineraryDto(DateTime GeneratedUtc, IReadOnlyList<ItinerarySectionDto> Sections);
