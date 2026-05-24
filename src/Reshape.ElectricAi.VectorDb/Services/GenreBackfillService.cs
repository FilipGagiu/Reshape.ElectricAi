using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Schema;
using Reshape.ElectricAi.VectorDb.Entities;
using Reshape.ElectricAi.VectorDb.Persistence.Specifications;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed partial class GenreBackfillService(
    IRepository<DocumentChunk> chunkRepository,
    IOpenAiClient openAi,
    ILogger<GenreBackfillService> logger)
{
    private const int BatchSize = 20;
    private const int MaxCompletionTokens = 200;
    private const double Temperature = 0.0;

    private static readonly JsonNode ResponseSchema = JsonSchemaStrictifier.Apply(
        LlmJsonOptions.ExportSchema(typeof(ArtistGenreClassification)));

    public async Task<GenreBackfillResult> BackfillAsync(CancellationToken cancellationToken)
    {
        var chunks = await chunkRepository.ListAsync(new ArtistChunksMissingTagsSpec(), cancellationToken);

        LogScanFound(logger, chunks.Count, null);

        if (chunks.Count == 0)
            return new GenreBackfillResult(0, 0, 0);

        var processed = 0;
        var failed = 0;
        var sinceLastSave = 0;

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await openAi.CompleteStructuredAsync<ArtistGenreClassification>(
                    systemPrompt: GenreBackfillSchema.SystemPrompt,
                    userPrompt: chunk.Content,
                    responseSchema: ResponseSchema,
                    model: null,
                    maxCompletionTokens: MaxCompletionTokens,
                    temperature: Temperature,
                    cancellationToken: cancellationToken);

                var picked = result.Value.Genres
                    .Where(g => Enum.IsDefined(g))
                    .Select(g => g.ToString())
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                if (picked.Count == 0)
                {
                    failed++;
                    LogChunkNoGenres(logger, chunk.Id, null);
                    continue;
                }

                chunk.CategoryTags = CategoryTagsHelper.ToTags(
                    new Dictionary<Category, IReadOnlyList<string>> { [Category.Music] = picked });

                processed++;
                sinceLastSave++;
            }
            catch (LlmException ex)
            {
                failed++;
                LogChunkLlmFailed(logger, chunk.Id, ex);
                continue;
            }

            if (sinceLastSave >= BatchSize)
            {
                await chunkRepository.SaveChangesAsync(cancellationToken);
                LogBatchSaved(logger, sinceLastSave, null);
                sinceLastSave = 0;
            }
        }

        if (sinceLastSave > 0)
        {
            await chunkRepository.SaveChangesAsync(cancellationToken);
            LogBatchSaved(logger, sinceLastSave, null);
        }

        var skipped = chunks.Count - processed - failed;
        return new GenreBackfillResult(processed, skipped, failed);
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information,
        Message = "Genre backfill scan: {Count} artist chunks missing tags")]
    private static partial void LogScanFound(ILogger logger, int count, Exception? ex);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information,
        Message = "Genre backfill batch saved: {Count} chunks")]
    private static partial void LogBatchSaved(ILogger logger, int count, Exception? ex);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Warning,
        Message = "Genre backfill chunk LLM failed: {ChunkId}")]
    private static partial void LogChunkLlmFailed(ILogger logger, Guid chunkId, Exception ex);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Warning,
        Message = "Genre backfill chunk returned no valid genres: {ChunkId}")]
    private static partial void LogChunkNoGenres(ILogger logger, Guid chunkId, Exception? ex);
}

public sealed record GenreBackfillResult(int Processed, int Skipped, int Failed);
