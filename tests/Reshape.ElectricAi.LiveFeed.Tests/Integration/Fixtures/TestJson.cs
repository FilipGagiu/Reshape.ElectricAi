using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

// Shared JsonSerializerOptions for test deserialization.
// Server emits camelCase enum strings (FeedController._jsonOpts + global controllers
// JsonStringEnumConverter wiring). Default ReadFromJsonAsync uses defaults that
// deserialize enums as integers and fails on string "weather", "music", etc.
internal static class TestJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
