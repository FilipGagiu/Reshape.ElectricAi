namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItineraryRefineRequest(Guid ItineraryId, string FreeText, string? Locale);
