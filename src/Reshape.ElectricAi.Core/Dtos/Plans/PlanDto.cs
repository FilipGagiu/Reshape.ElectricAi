using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Plans;

public sealed record PlanDto(
    Guid Id,
    PlanScope Scope,
    PlanState State,
    TicketType TicketType,
    IReadOnlyList<PlanDayDto> Days,
    IReadOnlyList<PlanFoodDto> Food,
    PlanBudgetDto Budget,
    DateTime? ExportedUtc);
