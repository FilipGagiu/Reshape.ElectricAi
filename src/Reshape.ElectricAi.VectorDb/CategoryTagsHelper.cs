using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.VectorDb;

internal static class CategoryTagsHelper
{
    internal static string[] ToTags(IReadOnlyDictionary<Category, IReadOnlyList<string>> categoryValues) =>
        categoryValues
            .SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}.{v}"))
            .ToArray();

    internal static IReadOnlyDictionary<Category, IReadOnlyList<string>> FromTags(string[] tags)
    {
        var result = new Dictionary<Category, List<string>>();
        foreach (var tag in tags)
        {
            var dot = tag.IndexOf('.');
            if (dot < 0) continue;
            if (!Enum.TryParse<Category>(tag[..dot], out var category)) continue;
            if (!result.TryGetValue(category, out var list))
            {
                list = [];
                result[category] = list;
            }
            list.Add(tag[(dot + 1)..]);
        }
        return result.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());
    }
}
