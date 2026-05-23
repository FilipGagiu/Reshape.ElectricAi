using System.Text.Json.Nodes;

namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItinerarySectionResult(string Key, int Order, JsonNode Data, string? Diagnostic);
