using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed class EcDataSeeder(IIngestService ingest, ILogger<EcDataSeeder> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly Action<ILogger, int, Exception?> LogFaqSeeded =
        LoggerMessage.Define<int>(LogLevel.Information, 0, "EC FAQ seeder: processed {Count} entries");

    private static readonly Action<ILogger, int, Exception?> LogPagesSeeded =
        LoggerMessage.Define<int>(LogLevel.Information, 0, "EC pages seeder: processed {Count} sections");

    public async Task SeedAsync(string dataRoot, CancellationToken ct = default)
    {
        var faqPath = Path.Combine(dataRoot, "faqs-ec-website.json");
        if (File.Exists(faqPath))
            await SeedFaqsAsync(faqPath, ct);

        var pagesDir = Path.Combine(dataRoot, "ec-pages");
        if (Directory.Exists(pagesDir))
            await SeedPagesAsync(pagesDir, ct);
    }

    private async Task SeedFaqsAsync(string jsonPath, CancellationToken ct)
    {
        var entries = JsonSerializer.Deserialize<FaqEntry[]>(
            await File.ReadAllTextAsync(jsonPath, ct), JsonOpts)!;

        foreach (var entry in entries)
        {
            await ingest.IngestQAAsync(new IngestQARequest(
                QuestionText: entry.Question,
                Answers: entry.Answers.Select(a => new IngestAnswerRequest(a.Text)).ToList(),
                QuestionCategoryValues: ToCategoryValues(entry.QuestionCategoryValues)), ct);
        }

        LogFaqSeeded(logger, entries.Length, null);
    }

    private async Task SeedPagesAsync(string pagesDir, CancellationToken ct)
    {
        var sectionCount = 0;

        foreach (var file in Directory.EnumerateFiles(pagesDir, "*.md"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name is "faq" or "README") continue;

            var raw = await File.ReadAllTextAsync(file, ct);
            if (raw.Contains("WARNING:") || raw.Contains("ERROR:")) continue;

            var (sourceRef, body) = ParseFrontmatter(raw);
            var docCat = InferDocCategoryValues(name);

            foreach (var (heading, content) in SplitSections(body))
            {
                var title = string.IsNullOrEmpty(heading) ? sourceRef : $"{sourceRef}#{heading}";
                await ingest.IngestDocumentAsync(new IngestDocumentRequest(
                    Title: title,
                    Content: content,
                    CategoryValues: docCat), ct);
                sectionCount++;
            }
        }

        LogPagesSeeded(logger, sectionCount, null);
    }

    private static (string sourceRef, string body) ParseFrontmatter(string content)
    {
        if (!content.StartsWith("---\n", StringComparison.Ordinal))
            return ("unknown", content);

        var end = content.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (end < 0)
            return ("unknown", content);

        var frontmatter = content[4..end];
        var body = content[(end + 5)..].TrimStart('\n');

        var sourceRef = frontmatter
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("source_ref:", StringComparison.Ordinal))
            .Select(l => l["source_ref:".Length..].Trim())
            .FirstOrDefault() ?? "unknown";

        return (sourceRef, body);
    }

    private static List<(string heading, string content)> SplitSections(string body)
    {
        var result = new List<(string, string)>();

        // Prepend newline so a leading ## at the very start of body is caught by the split
        var parts = ("\n" + body).Split("\n## ");

        var preamble = parts[0].TrimStart('\n');
        if (!string.IsNullOrWhiteSpace(preamble))
            result.Add((string.Empty, preamble));

        foreach (var part in parts.Skip(1))
        {
            var newline = part.IndexOf('\n');
            var heading = newline >= 0 ? part[..newline].Trim() : part.Trim();
            result.Add((heading, "## " + part));
        }

        return result;
    }

    private static Dictionary<Category, IReadOnlyList<string>>? InferDocCategoryValues(
        string filename) =>
        filename switch
        {
            "ec-village"     => Dict(Category.Accommodation, "VillageRental", "Camping", "CarCamping", "RvCamping", "Glamping"),
            "music-stages"   => Dict(Category.Music, "HipHop", "House", "Balkan", "Rock", "Folk", "Techno", "Pop", "Electronic", "Jazz", "Metal", "Other"),
            "line-up"        => Dict(Category.Music, "HipHop", "House", "Balkan", "Rock", "Folk", "Techno", "Pop", "Electronic", "Jazz", "Metal", "Other"),
            "vip-experience" => Dict(Category.Ticket, "Vip", "UltraVip", "Black"),
            "youth-pass-u25" => Dict(Category.Ticket, "Standard"),
            "international"  => Dict(Category.Ticket, "Standard", "Vip", "UltraVip", "Black"),
            "fyi"            => Dict(Category.Ticket, "Standard", "Vip", "UltraVip", "Black"),
            "sustainability" => Dict(Category.Ticket, "Standard", "Vip", "UltraVip", "Black"),
            _                => null,
        };

    private static Dictionary<Category, IReadOnlyList<string>>? ToCategoryValues(
        Dictionary<string, string[]>? raw)
    {
        if (raw is null || raw.Count == 0) return null;

        var result = new Dictionary<Category, IReadOnlyList<string>>(raw.Count);
        foreach (var (key, values) in raw)
        {
            if (Enum.TryParse<Category>(key, out var cat))
                result[cat] = values;
        }

        return result.Count > 0 ? result : null;
    }

    private static Dictionary<Category, IReadOnlyList<string>> Dict(
        Category cat, params string[] values) =>
        new Dictionary<Category, IReadOnlyList<string>> { [cat] = values };

    private sealed record FaqEntry(
        string SourceRef,
        string Section,
        string Question,
        Dictionary<string, string[]>? QuestionCategoryValues,
        FaqAnswer[] Answers);

    private sealed record FaqAnswer(string Text, Dictionary<string, string[]>? CategoryValues);
}
