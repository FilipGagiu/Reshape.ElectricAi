namespace Reshape.ElectricAi.Core.Dtos.Plans;

public sealed record PlanTransportLegDto(
    string Mode,
    string? From,
    string? DepartLocal,
    string? Note);
