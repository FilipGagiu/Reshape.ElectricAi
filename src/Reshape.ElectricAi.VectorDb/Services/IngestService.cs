using System.Security.Cryptography;
using System.Text;
using Microsoft.ML.Tokenizers;
using Pgvector;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.VectorDb.Entities;
using Reshape.ElectricAi.VectorDb.Persistence.Specifications;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed class IngestService(
    IRepository<Document> documentRepository,
    IRepository<Question> questionRepository,
    IRepository<EventEntry> eventRepository,
    IEmbeddingService embeddingService) : IIngestService
{
    private const int MaxChunkTokens = 400;
    private const int ChunkOverlapTokens = 50;

    private static readonly TiktokenTokenizer Tokenizer =
        TiktokenTokenizer.CreateForModel("text-embedding-3-small");

    public async Task IngestDocumentAsync(IngestDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var sourceHash = ComputeHash(request.Content);

        if (await documentRepository.AnyAsync(new DocumentBySourceHashSpec(sourceHash), cancellationToken))
            return;

        var chunkTexts = Chunk(request.Content);
        var embeddings = await embeddingService.GenerateEmbeddingsAsync(chunkTexts, cancellationToken);

        var tags = request.CategoryValues is not null
            ? CategoryTagsHelper.ToTags(request.CategoryValues)
            : [];

        var document = new Document
        {
            Title = request.Title,
            SourceHash = sourceHash,
            IngestedUtc = DateTimeOffset.UtcNow,
        };

        for (var i = 0; i < chunkTexts.Count; i++)
        {
            document.Chunks.Add(new DocumentChunk
            {
                Content = chunkTexts[i],
                Embedding = new Vector(embeddings[i].ToArray()),
                CategoryTags = tags,
                ChunkIndex = i,
            });
        }

        await documentRepository.AddAsync(document, cancellationToken);
        await documentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task IngestQAAsync(IngestQARequest request, CancellationToken cancellationToken = default)
    {
        var textHash = ComputeHash(request.QuestionText);

        if (await questionRepository.AnyAsync(new QuestionByTextHashSpec(textHash), cancellationToken))
            return;

        var questionEmbedding = await embeddingService.GenerateEmbeddingAsync(request.QuestionText, cancellationToken);

        var answerTexts = request.Answers.Select(a => a.AnswerText).ToList();
        var answerEmbeddings = await embeddingService.GenerateEmbeddingsAsync(answerTexts, cancellationToken);

        var questionTags = request.QuestionCategoryValues is not null
            ? CategoryTagsHelper.ToTags(request.QuestionCategoryValues)
            : [];

        var question = new Question
        {
            Text = request.QuestionText,
            TextHash = textHash,
            Embedding = new Vector(questionEmbedding.ToArray()),
            CategoryTags = questionTags,
            IngestedUtc = DateTimeOffset.UtcNow,
        };

        for (var i = 0; i < request.Answers.Count; i++)
        {
            var answerRequest = request.Answers[i];
            var answerTags = answerRequest.CategoryValues is not null
                ? CategoryTagsHelper.ToTags(answerRequest.CategoryValues)
                : [];

            question.Answers.Add(new Answer
            {
                Text = answerRequest.AnswerText,
                Embedding = new Vector(answerEmbeddings[i].ToArray()),
                CategoryTags = answerTags,
                IngestedUtc = DateTimeOffset.UtcNow,
            });
        }

        await questionRepository.AddAsync(question, cancellationToken);
        await questionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task IngestEventAsync(IngestEventRequest request, CancellationToken cancellationToken = default)
    {
        if (await eventRepository.AnyAsync(new EventByFeedEntryIdSpec(request.FeedEntryId), cancellationToken))
            return;

        var embedding = await embeddingService.GenerateEmbeddingAsync(request.TextRepresentation, cancellationToken);

        var tags = request.CategoryValues is not null
            ? CategoryTagsHelper.ToTags(request.CategoryValues)
            : [];

        var entry = new EventEntry
        {
            FeedEntryId = request.FeedEntryId,
            Title = request.Title,
            TextRepresentation = request.TextRepresentation,
            Embedding = new Vector(embedding.ToArray()),
            CategoryTags = tags,
            EventUtc = request.EventUtc,
            IngestedUtc = DateTimeOffset.UtcNow,
        };

        await eventRepository.AddAsync(entry, cancellationToken);
        await eventRepository.SaveChangesAsync(cancellationToken);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static List<string> Chunk(string text)
    {
        var ids = Tokenizer.EncodeToIds(text).ToList();

        if (ids.Count <= MaxChunkTokens)
            return [text];

        var chunks = new List<string>();
        var i = 0;

        while (i < ids.Count)
        {
            var length = Math.Min(MaxChunkTokens, ids.Count - i);
            chunks.Add(Tokenizer.Decode(ids.GetRange(i, length)) ?? string.Empty);
            if (i + length >= ids.Count) break;
            i += MaxChunkTokens - ChunkOverlapTokens;
        }

        return chunks;
    }
}
