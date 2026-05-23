namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record WizardAnswer(string Question, string Answer, DateTimeOffset? AnsweredAt);
