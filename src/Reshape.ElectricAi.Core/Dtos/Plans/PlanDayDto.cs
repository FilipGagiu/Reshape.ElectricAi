namespace Reshape.ElectricAi.Core.Dtos.Plans;

public sealed record PlanDayDto(
    DateOnly Date,
    PlanTransportDto Transport,
    IReadOnlyList<PlanConcertDto> Concerts,
    IReadOnlyList<PlanActivityDto> Activities,
    IReadOnlyList<string> WeatherNotes);
