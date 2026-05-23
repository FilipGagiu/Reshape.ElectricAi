namespace Reshape.ElectricAi.Core.Dtos.Plans;

public sealed record PlanTransportDto(
    PlanTransportLegDto Outbound,
    PlanTransportLegDto Return);
