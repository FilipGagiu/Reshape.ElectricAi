using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Services.Schema;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services.Schema;

public class JsonSchemaStrictifierTests
{
    [Fact]
    public void Bare_object_gains_additionalProperties_false_and_full_required()
    {
        var schema = JsonNode.Parse("""
            { "type": "object", "properties": { "a": { "type": "string" } } }
            """)!;

        var result = JsonSchemaStrictifier.Apply(schema);

        Assert.False(result["additionalProperties"]!.GetValue<bool>());
        var required = result["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "a" }, required);
        // "a" was not in original required -> widened to union with null.
        var aType = result["properties"]!["a"]!["type"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "string", "null" }, aType);
    }

    [Fact]
    public void Nested_object_under_properties_is_transformed()
    {
        var schema = JsonNode.Parse("""
            {
              "type": "object",
              "properties": {
                "outer": {
                  "type": "object",
                  "properties": { "inner": { "type": "string" } }
                }
              },
              "required": ["outer"]
            }
            """)!;

        var result = JsonSchemaStrictifier.Apply(schema);

        var outer = result["properties"]!["outer"]!;
        Assert.False(outer["additionalProperties"]!.GetValue<bool>());
        var required = outer["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "inner" }, required);
    }

    [Fact]
    public void Object_under_defs_is_transformed()
    {
        var schema = JsonNode.Parse("""
            {
              "type": "object",
              "properties": { "foo": { "$ref": "#/$defs/Foo" } },
              "required": ["foo"],
              "$defs": {
                "Foo": {
                  "type": "object",
                  "properties": { "bar": { "type": "integer" } }
                }
              }
            }
            """)!;

        var result = JsonSchemaStrictifier.Apply(schema);

        var foo = result["$defs"]!["Foo"]!;
        Assert.False(foo["additionalProperties"]!.GetValue<bool>());
        var required = foo["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "bar" }, required);
    }

    [Fact]
    public void Nullable_string_widens_type_and_adds_to_required()
    {
        var schema = JsonNode.Parse("""
            { "type": "object", "properties": { "name": { "type": "string" } } }
            """)!;

        var result = JsonSchemaStrictifier.Apply(schema);

        var nameType = result["properties"]!["name"]!["type"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "string", "null" }, nameType);
        Assert.Contains("name", result["required"]!.AsArray().Select(n => n!.GetValue<string>()));
    }

    [Fact]
    public void Nullable_enum_widens_type_keeps_enum_values()
    {
        var schema = JsonNode.Parse("""
            {
              "type": "object",
              "properties": { "color": { "type": "string", "enum": ["Red", "Blue"] } }
            }
            """)!;

        var result = JsonSchemaStrictifier.Apply(schema);

        var color = result["properties"]!["color"]!;
        var colorType = color["type"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "string", "null" }, colorType);
        var enumValues = color["enum"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "Red", "Blue" }, enumValues);
    }

    [Fact]
    public void Nullable_ref_property_wraps_in_anyOf()
    {
        var schema = JsonNode.Parse("""
            {
              "type": "object",
              "properties": { "foo": { "$ref": "#/$defs/Foo" } },
              "$defs": { "Foo": { "type": "object", "properties": {} } }
            }
            """)!;

        var result = JsonSchemaStrictifier.Apply(schema);

        var foo = result["properties"]!["foo"]!;
        var anyOf = foo["anyOf"]!.AsArray();
        Assert.Equal(2, anyOf.Count);
        Assert.Equal("#/$defs/Foo", anyOf[0]!["$ref"]!.GetValue<string>());
        Assert.Equal("null", anyOf[1]!["type"]!.GetValue<string>());
        Assert.Null(foo["$ref"]);
    }

    [Fact]
    public void Nullable_array_property_widens_array_type()
    {
        var schema = JsonNode.Parse("""
            {
              "type": "object",
              "properties": {
                "tags": { "type": "array", "items": { "type": "string" } }
              }
            }
            """)!;

        var result = JsonSchemaStrictifier.Apply(schema);

        var tagsType = result["properties"]!["tags"]!["type"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "array", "null" }, tagsType);
        Assert.Equal("string", result["properties"]!["tags"]!["items"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void Already_nullable_int_no_double_null_still_required()
    {
        var schema = JsonNode.Parse("""
            {
              "type": "object",
              "properties": { "age": { "type": ["integer", "null"] } }
            }
            """)!;

        var result = JsonSchemaStrictifier.Apply(schema);

        var ageType = result["properties"]!["age"]!["type"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "integer", "null" }, ageType);
        var required = result["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Contains("age", required);
    }

    [Fact]
    public void Required_properties_unchanged()
    {
        var schema = JsonNode.Parse("""
            {
              "type": "object",
              "properties": { "id": { "type": "string" }, "name": { "type": "string" } },
              "required": ["id"]
            }
            """)!;

        var result = JsonSchemaStrictifier.Apply(schema);

        // id was already required -> stays bare "string", not unioned with null.
        Assert.Equal("string", result["properties"]!["id"]!["type"]!.GetValue<string>());
        // name was not required -> widened.
        var nameType = result["properties"]!["name"]!["type"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "string", "null" }, nameType);
        // Both end up in required.
        var required = result["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "id", "name" }, required);
    }

    [Fact]
    public void End_to_end_AiExtractedPreferences_satisfies_strict_rules()
    {
        var raw = JsonSchemaExporter.GetJsonSchemaAsNode(LlmJsonOptions.Default, typeof(AiExtractedPreferences));
        var strict = JsonSchemaStrictifier.Apply(raw);

        AssertStrictlyCompliant(strict);
    }

    [Fact]
    public void Idempotent()
    {
        var raw = JsonSchemaExporter.GetJsonSchemaAsNode(LlmJsonOptions.Default, typeof(AiExtractedPreferences));
        var once = JsonSchemaStrictifier.Apply(JsonNode.Parse(raw.ToJsonString())!);
        var twice = JsonSchemaStrictifier.Apply(JsonNode.Parse(once.ToJsonString())!);

        Assert.Equal(once.ToJsonString(), twice.ToJsonString());
    }

    private static void AssertStrictlyCompliant(JsonNode? node)
    {
        switch (node)
        {
            case JsonArray arr:
                foreach (var item in arr)
                {
                    AssertStrictlyCompliant(item);
                }
                break;
            case JsonObject obj:
                if (IsObjectSchema(obj))
                {
                    Assert.True(obj.ContainsKey("additionalProperties"),
                        $"Object schema missing additionalProperties: {obj.ToJsonString()}");
                    Assert.False(obj["additionalProperties"]!.GetValue<bool>());

                    var propNames = obj["properties"]!.AsObject().Select(kv => kv.Key).ToArray();
                    Assert.True(obj.ContainsKey("required"),
                        $"Object schema missing required: {obj.ToJsonString()}");
                    var required = obj["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
                    Assert.Equal(propNames.OrderBy(x => x), required.OrderBy(x => x));
                }
                foreach (var kv in obj)
                {
                    AssertStrictlyCompliant(kv.Value);
                }
                break;
            default:
                break;
        }
    }

    private static bool IsObjectSchema(JsonObject obj)
    {
        if (obj["properties"] is not JsonObject)
            return false;
        var type = obj["type"];
        if (type is JsonValue v && v.TryGetValue<string>(out var s) && s == "object")
            return true;
        if (type is JsonArray arr && arr.Any(n => n is JsonValue jv && jv.TryGetValue<string>(out var ss) && ss == "object"))
            return true;
        return false;
    }
}
