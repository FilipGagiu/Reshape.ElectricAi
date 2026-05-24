using System.Text.Json.Nodes;

namespace Reshape.ElectricAi.Core.Services.Schema;

/// <summary>
/// Rewrites a <see cref="JsonNode"/> JSON Schema in-place so OpenAI's structured-output
/// strict mode accepts it. Strict mode requires <c>additionalProperties: false</c> on every
/// object schema, every property listed in <c>required</c>, and nullability expressed as a
/// type union with <c>"null"</c> (or an <c>anyOf</c> with <c>{"type":"null"}</c> for
/// <c>$ref</c>-only properties). <see cref="System.Text.Json.Schema.JsonSchemaExporter"/>
/// does not emit any of these by default.
/// </summary>
public static class JsonSchemaStrictifier
{
    private static readonly string[] CombinatorKeywords = ["anyOf", "oneOf", "allOf"];

    /// <summary>
    /// Walks <paramref name="root"/> recursively, mutating in place, and returns the same
    /// reference. Safe to call multiple times (idempotent).
    /// </summary>
    public static JsonNode Apply(JsonNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Visit(root);
        return root;
    }

    private static void Visit(JsonNode? node)
    {
        switch (node)
        {
            case JsonArray arr:
                foreach (var item in arr)
                {
                    Visit(item);
                }
                return;
            case JsonObject obj:
                VisitObject(obj);
                return;
            default:
                return;
        }
    }

    private static void VisitObject(JsonObject obj)
    {
        if (obj["properties"] is JsonObject props)
        {
            foreach (var kv in props)
            {
                Visit(kv.Value);
            }
        }
        if (obj["items"] is JsonNode items)
        {
            Visit(items);
        }
        if (obj["$defs"] is JsonObject defs)
        {
            foreach (var kv in defs)
            {
                Visit(kv.Value);
            }
        }
        foreach (var keyword in CombinatorKeywords)
        {
            if (obj[keyword] is JsonArray combinators)
            {
                foreach (var item in combinators)
                {
                    Visit(item);
                }
            }
        }

        if (!TypeIncludes(obj, "object") || obj["properties"] is not JsonObject properties)
        {
            return;
        }

        obj["additionalProperties"] = false;

        var originalRequired = new HashSet<string>(StringComparer.Ordinal);
        if (obj["required"] is JsonArray reqArr)
        {
            foreach (var n in reqArr)
            {
                if (n is JsonValue jv && jv.TryGetValue<string>(out var s))
                {
                    originalRequired.Add(s);
                }
            }
        }

        var propNames = new List<string>();
        foreach (var kv in properties)
        {
            propNames.Add(kv.Key);
            if (!originalRequired.Contains(kv.Key) && kv.Value is not null)
            {
                WidenForNullable(kv.Value);
            }
        }

        var newRequired = new JsonArray();
        foreach (var name in propNames)
        {
            newRequired.Add(name);
        }
        obj["required"] = newRequired;
    }

    private static void WidenForNullable(JsonNode propNode)
    {
        if (propNode is not JsonObject obj)
        {
            return;
        }

        if (obj["$ref"] is JsonValue refVal && refVal.TryGetValue<string>(out var refStr) && OnlyRefKey(obj))
        {
            obj.Remove("$ref");
            obj["anyOf"] = new JsonArray(
                new JsonObject { ["$ref"] = refStr },
                new JsonObject { ["type"] = "null" });
            return;
        }

        if (obj["anyOf"] is JsonArray existingAnyOf && AnyOfIncludesNull(existingAnyOf))
        {
            return;
        }

        var typeNode = obj["type"];
        switch (typeNode)
        {
            case JsonValue tv when tv.TryGetValue<string>(out var tStr):
                if (tStr != "null")
                {
                    obj["type"] = new JsonArray(tStr, "null");
                }
                return;
            case JsonArray tArr:
                if (!ArrayContainsString(tArr, "null"))
                {
                    tArr.Add("null");
                }
                return;
            case null:
                var clone = obj.DeepClone();
                foreach (var key in obj.Select(kv => kv.Key).ToArray())
                {
                    obj.Remove(key);
                }
                obj["anyOf"] = new JsonArray(clone, new JsonObject { ["type"] = "null" });
                return;
            default:
                return;
        }
    }

    private static bool TypeIncludes(JsonObject obj, string typeName)
    {
        var typeNode = obj["type"];
        return typeNode switch
        {
            JsonValue v when v.TryGetValue<string>(out var s) => s == typeName,
            JsonArray arr => ArrayContainsString(arr, typeName),
            _ => false
        };
    }

    private static bool OnlyRefKey(JsonObject obj)
    {
        var count = 0;
        foreach (var kv in obj)
        {
            count++;
            if (count > 1)
            {
                return false;
            }
            if (kv.Key != "$ref")
            {
                return false;
            }
        }
        return count == 1;
    }

    private static bool AnyOfIncludesNull(JsonArray anyOf)
    {
        foreach (var item in anyOf)
        {
            if (item is JsonObject candidate
                && candidate["type"] is JsonValue v
                && v.TryGetValue<string>(out var s)
                && s == "null")
            {
                return true;
            }
        }
        return false;
    }

    private static bool ArrayContainsString(JsonArray arr, string value)
    {
        foreach (var item in arr)
        {
            if (item is JsonValue v && v.TryGetValue<string>(out var s) && s == value)
            {
                return true;
            }
        }
        return false;
    }
}
