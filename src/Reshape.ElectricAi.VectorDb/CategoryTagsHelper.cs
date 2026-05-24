using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.VectorDb;

internal static class CategoryTagsHelper
{
    internal static string[] ToTags(IReadOnlyDictionary<Category, IReadOnlyList<string>> categoryValues) =>
        categoryValues
            .SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}.{v}".ToLowerInvariant()))
            .ToArray();

}
