using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Reshape.ElectricAi.Core.Configuration;

public static class LlmJsonOptions
{
    public static readonly JsonSerializerOptions Default = CreateReadOnly();

    public static JsonNode ExportSchema(Type type) =>
        JsonSchemaExporter.GetJsonSchemaAsNode(Default, type);

    private static JsonSerializerOptions CreateReadOnly()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            Converters = { new JsonStringEnumConverter() }
        };
        options.MakeReadOnly();
        return options;
    }
}
