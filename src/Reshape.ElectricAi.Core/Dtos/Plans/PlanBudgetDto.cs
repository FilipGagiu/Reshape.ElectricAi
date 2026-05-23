namespace Reshape.ElectricAi.Core.Dtos.Plans;

public sealed record PlanBudgetDto(
    int Ticket,
    int Transport,
    int Accommodation,
    int Food,
    int Drinks,
    int ChaosFund,
    int Total,
    string Currency);
