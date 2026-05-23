using Reshape.ElectricAi.Core.Dtos.Plans;
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Services.Generation;

internal sealed record AiPlan
{
    // Scope intentionally omitted — PlanGenerator hardcodes PlanScope.Individual
    // for this slice. Add back when group plan generation lands.
    public TicketType? TicketType { get; init; }
    public List<PlanDayDto>? Days { get; init; }
    public List<PlanFoodDto>? Food { get; init; }
    public PlanBudgetDto? Budget { get; init; }
}
