namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItineraryDto(Guid Id, DateTime GeneratedUtc, IReadOnlyList<ItinerarySectionDto> Sections);
