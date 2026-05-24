using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.VectorDb;

internal static class CategoryTagsHelper
{
    internal static string[] ToTags(IReadOnlyDictionary<Category, IReadOnlyList<string>> categoryValues) =>
        categoryValues
            .SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}.{v}".ToLowerInvariant()))
            .ToArray();

    internal static IReadOnlyList<CategoryTagFilter> ToPerCategoryTagFilters(
        IReadOnlyDictionary<Category, IReadOnlyList<string>> categoryValues) =>
        categoryValues
            .Select(kvp => new CategoryTagFilter(
                CategoryPrefix: $"{kvp.Key}.".ToLowerInvariant(),
                AllowedTags: kvp.Value
                    .Select(v => $"{kvp.Key}.{v}".ToLowerInvariant())
                    .ToArray()))
            .ToList();
}

internal readonly record struct CategoryTagFilter(string CategoryPrefix, string[] AllowedTags);
