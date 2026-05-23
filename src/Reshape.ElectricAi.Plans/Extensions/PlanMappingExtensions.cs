using System.Text.Json;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Plans;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Extensions;

internal static class PlanMappingExtensions
{
    public static string SerializeContent(this PlanDto dto) =>
        JsonSerializer.Serialize(dto, LlmJsonOptions.Default);

    public static PlanDto? DeserializeContent(this Plan entity) =>
        string.IsNullOrWhiteSpace(entity.ContentJson)
            ? null
            : JsonSerializer.Deserialize<PlanDto>(entity.ContentJson, LlmJsonOptions.Default);
}
