using System.Text.Json.Nodes;

namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItinerarySectionDto(string Key, JsonNode Data, string? Diagnostic);
