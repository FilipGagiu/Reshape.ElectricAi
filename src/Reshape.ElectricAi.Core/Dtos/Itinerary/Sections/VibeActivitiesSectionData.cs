namespace Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;

public sealed record VibeActivitiesSectionData(
    IReadOnlyList<string> VibeTags,
    IReadOnlyList<RecommendedActivityDto> TopActivities);
