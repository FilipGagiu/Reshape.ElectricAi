using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Preferences;

public sealed record AiExtractedCrew(CrewKind Kind, int? EstimatedSize);
