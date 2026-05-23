namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItineraryGenerationRequest(
    int Version,
    string Locale,
    DateTimeOffset SubmittedAt,
    IReadOnlyList<WizardAnswer> Answers,
    string? FreeText);
