using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Itinerary;
using Reshape.ElectricAi.Core.Services.Schema;
using Reshape.ElectricAi.Plans.Configuration;

namespace Reshape.ElectricAi.Plans.Services.Itinerary;

internal sealed partial class PreferencesExtractor(
    IOpenAiClient openAi,
    IOptions<ItineraryGenerationOptions> options,
    ILogger<PreferencesExtractor> logger) : IPreferencesExtractor
{
    private static readonly string SystemPrompt = LoadEmbeddedPrompt();
    private static readonly JsonNode ResponseSchema = JsonSchemaStrictifier.Apply(
        LlmJsonOptions.ExportSchema(typeof(AiExtractedPreferences)));

    private readonly ItineraryGenerationOptions _options = options.Value;

    public async Task<AiExtractedPreferences> ExtractAsync(
        IReadOnlyList<WizardAnswer> answers,
        string? freeText,
        string locale,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        LogStarted(logger, answers.Count, freeText?.Length ?? 0, locale);

        var userPrompt = BuildUserPrompt(answers, freeText, locale);
        var llm = await openAi.CompleteStructuredAsync<AiExtractedPreferences>(
            SystemPrompt,
            userPrompt,
            ResponseSchema,
            _options.Model,
            _options.MaxCompletionTokens,
            _options.Temperature,
            cancellationToken);

        LogCompleted(logger, llm.Usage.PromptTokens, llm.Usage.CompletionTokens, llm.Usage.CostCents);
        return llm.Value;
    }

    private static string BuildUserPrompt(IReadOnlyList<WizardAnswer> answers, string? freeText, string locale)
    {
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine(ci, $"locale={locale}");
        sb.AppendLine("User answered the wizard like this:");
        sb.AppendLine();
        var i = 1;
        foreach (var a in answers)
        {
            sb.AppendLine(ci, $"{i++}. {a.Question}");
            sb.AppendLine(ci, $"   -> {a.Answer}");
            sb.AppendLine();
        }
        sb.AppendLine("Additional notes from the user:");
        sb.AppendLine(string.IsNullOrWhiteSpace(freeText) ? "(none)" : freeText);
        sb.AppendLine();
        sb.AppendLine("Extract the structured user preferences via the response tool. Emit null where uncertain.");
        return sb.ToString();
    }

    private static string LoadEmbeddedPrompt()
    {
        var asm = typeof(PreferencesExtractor).Assembly;
        const string resource = "Reshape.ElectricAi.Plans.Services.Prompts.PreferencesExtractorSystemPrompt.md";
        using var stream = asm.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded resource missing: {resource}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [LoggerMessage(EventId = 7101, Level = LogLevel.Information,
        Message = "PreferencesExtraction started answers={Answers} freeTextLength={FreeTextLength} locale={Locale}")]
    private static partial void LogStarted(ILogger logger, int answers, int freeTextLength, string locale);

    [LoggerMessage(EventId = 7102, Level = LogLevel.Information,
        Message = "PreferencesExtraction completed promptTokens={Prompt} completionTokens={Completion} costCents={Cost}")]
    private static partial void LogCompleted(ILogger logger, int prompt, int completion, int cost);
}
