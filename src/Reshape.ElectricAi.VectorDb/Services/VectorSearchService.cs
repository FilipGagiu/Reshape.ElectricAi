using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.VectorDb.Persistence;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed class VectorSearchService(VectorDbContext context, IEmbeddingService embeddingService) : IVectorSearchService
{
    public async Task<IReadOnlyList<RetrievedChunk>> SearchDocumentsAsync(
        DocumentSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(filter.QueryText, cancellationToken);
        var queryVector = new Vector(queryEmbedding.ToArray());

        var query = context.DocumentChunks.AsNoTracking();

        if (filter.UserContext is { Count: > 0 })
        {
            foreach (var f in CategoryTagsHelper.ToPerCategoryTagFilters(filter.UserContext))
            {
                var prefix = f.CategoryPrefix;
                var allowed = f.AllowedTags;
                query = query.Where(c =>
                    !c.CategoryTags.Any(t => EF.Functions.Like(t, prefix + "%")) ||
                    c.CategoryTags.Any(t => allowed.Contains(t)));
            }
        }

        var raw = await query
            .Select(c => new
            {
                c.DocumentId,
                c.Document!.Title,
                c.ChunkIndex,
                c.Content,
                Distance = c.Embedding.CosineDistance(queryVector),
            })
            .OrderBy(x => x.Distance)
            .Take(filter.TopK)
            .ToListAsync(cancellationToken);

        return raw
            .Select(x => new RetrievedChunk(x.DocumentId, x.Title, x.ChunkIndex, x.Content, (float)(1.0 - x.Distance)))
            .ToList();
    }

    public async Task<IReadOnlyList<RetrievedQA>> SearchQuestionsAsync(
        QuestionSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(filter.QueryText, cancellationToken);
        var queryVector = new Vector(queryEmbedding.ToArray());

        var query = context.Questions.AsNoTracking();

        if (filter.UserContext is { Count: > 0 })
        {
            foreach (var f in CategoryTagsHelper.ToPerCategoryTagFilters(filter.UserContext))
            {
                var prefix = f.CategoryPrefix;
                var allowed = f.AllowedTags;
                query = query.Where(q =>
                    !q.CategoryTags.Any(t => EF.Functions.Like(t, prefix + "%")) ||
                    q.CategoryTags.Any(t => allowed.Contains(t)));
            }
        }

        var questionResults = await query
            .Select(q => new
            {
                q.Id,
                q.Text,
                Distance = q.Embedding.CosineDistance(queryVector),
            })
            .OrderBy(x => x.Distance)
            .Take(filter.TopK)
            .ToListAsync(cancellationToken);

        var questionIds = questionResults.Select(q => q.Id).ToList();

        var answerRows = await context.Answers
            .AsNoTracking()
            .Where(a => questionIds.Contains(a.QuestionId))
            .Select(a => new { a.QuestionId, a.Text })
            .ToListAsync(cancellationToken);

        var answersByQuestion = answerRows
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Select(a => a.Text).ToList());

        return questionResults
            .Select(q =>
            {
                var score = (float)(1.0 - q.Distance);
                var answers = answersByQuestion.GetValueOrDefault(q.Id, [])
                    .Select(text => new RetrievedAnswer(text, score))
                    .ToList();
                return new RetrievedQA(q.Text, answers, score);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<RetrievedEvent>> SearchEventsAsync(
        EventSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(filter.QueryText, cancellationToken);
        var queryVector = new Vector(queryEmbedding.ToArray());

        var query = context.EventEntries.AsNoTracking();

        if (filter.UserContext is { Count: > 0 })
        {
            foreach (var f in CategoryTagsHelper.ToPerCategoryTagFilters(filter.UserContext))
            {
                var prefix = f.CategoryPrefix;
                var allowed = f.AllowedTags;
                query = query.Where(e =>
                    !e.CategoryTags.Any(t => EF.Functions.Like(t, prefix + "%")) ||
                    e.CategoryTags.Any(t => allowed.Contains(t)));
            }
        }

        var raw = await query
            .Select(e => new
            {
                e.FeedEntryId,
                e.Title,
                e.TextRepresentation,
                e.EventUtc,
                Distance = e.Embedding.CosineDistance(queryVector),
            })
            .OrderBy(x => x.Distance)
            .Take(filter.TopK)
            .ToListAsync(cancellationToken);

        return raw
            .Select(x => new RetrievedEvent(x.FeedEntryId, x.Title, x.TextRepresentation, x.EventUtc, (float)(1.0 - x.Distance)))
            .ToList();
    }
}
