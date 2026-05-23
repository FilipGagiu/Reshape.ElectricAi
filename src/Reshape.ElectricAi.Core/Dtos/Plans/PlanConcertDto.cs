namespace Reshape.ElectricAi.Core.Dtos.Plans;

public sealed record PlanConcertDto(
    string Stage,
    string Artist,
    string StartLocal,
    string EndLocal);
