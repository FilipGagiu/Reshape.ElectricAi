using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.VectorDb.Entities;
using Reshape.ElectricAi.VectorDb.Persistence;
using Reshape.ElectricAi.VectorDb.Services;

namespace Reshape.ElectricAi.VectorDb.Tests;

[Collection("VectorDb")]
public sealed class IngestServiceTests(VectorDbFixture fixture)
{
    private IngestService BuildIngestService(VectorDbContext context) =>
        new(
            new VectorRepository<Document>(context),
            new VectorRepository<Question>(context),
            new VectorRepository<EventEntry>(context),
            fixture.CreateEmbeddingService());

    [Fact]
    public async Task IngestDocumentAsync_WithNewContent_CreatesDocumentAndChunks()
    {
        await using var context = fixture.CreateContext();
        var service = BuildIngestService(context);

        var request = new IngestDocumentRequest(
            Title: "Electric Castle FAQ",
            Content: "Electric Castle is a music festival held in Romania every summer.");

        await service.IngestDocumentAsync(request);

        var document = await context.Documents.Include(d => d.Chunks).SingleOrDefaultAsync(d => d.Title == "Electric Castle FAQ");
        document.Should().NotBeNull();
        document!.Chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task IngestDocumentAsync_WithSameContent_IsIdempotent()
    {
        await using var context = fixture.CreateContext();
        var service = BuildIngestService(context);

        var content = $"Idempotent document content {Guid.NewGuid()}";
        var request = new IngestDocumentRequest(Title: "Test Doc", Content: content);

        await service.IngestDocumentAsync(request);
        await service.IngestDocumentAsync(request);

        var count = await context.Documents.CountAsync(d => d.Title == "Test Doc" && d.SourceHash == ComputeHash(content));
        count.Should().Be(1);
    }

    [Fact]
    public async Task IngestQAAsync_WithNewQuestion_CreatesQuestionWithAnswers()
    {
        await using var context = fixture.CreateContext();
        var service = BuildIngestService(context);

        var questionText = $"What time does the festival start? {Guid.NewGuid()}";
        var request = new IngestQARequest(
            QuestionText: questionText,
            Answers: [new IngestAnswerRequest("Gates open at 14:00")]);

        await service.IngestQAAsync(request);

        var question = await context.Questions.Include(q => q.Answers).SingleOrDefaultAsync(q => q.Text == questionText);
        question.Should().NotBeNull();
        question!.Answers.Should().HaveCount(1);
        question.Answers.First().Text.Should().Be("Gates open at 14:00");
    }

    [Fact]
    public async Task IngestQAAsync_WithSameQuestion_IsIdempotent()
    {
        await using var context = fixture.CreateContext();
        var service = BuildIngestService(context);

        var questionText = $"Is parking available? {Guid.NewGuid()}";
        var request = new IngestQARequest(
            QuestionText: questionText,
            Answers: [new IngestAnswerRequest("Yes, on-site parking is available.")]);

        await service.IngestQAAsync(request);
        await service.IngestQAAsync(request);

        var count = await context.Questions.CountAsync(q => q.Text == questionText);
        count.Should().Be(1);
    }

    [Fact]
    public async Task IngestEventAsync_WithNewEntry_CreatesEventEntry()
    {
        await using var context = fixture.CreateContext();
        var service = BuildIngestService(context);

        var feedEntryId = Guid.NewGuid();
        var request = new IngestEventRequest(
            FeedEntryId: feedEntryId,
            Title: "Metallica at Main Stage",
            TextRepresentation: "Metallica performs at Main Stage on Saturday at 22:00.",
            EventUtc: DateTimeOffset.UtcNow.AddDays(30));

        await service.IngestEventAsync(request);

        var entry = await context.EventEntries.SingleOrDefaultAsync(e => e.FeedEntryId == feedEntryId);
        entry.Should().NotBeNull();
        entry!.Title.Should().Be("Metallica at Main Stage");
    }

    [Fact]
    public async Task IngestEventAsync_WithSameFeedEntryId_IsIdempotent()
    {
        await using var context = fixture.CreateContext();
        var service = BuildIngestService(context);

        var feedEntryId = Guid.NewGuid();
        var request = new IngestEventRequest(
            FeedEntryId: feedEntryId,
            Title: "Duplicate Event",
            TextRepresentation: "Some event description.",
            EventUtc: DateTimeOffset.UtcNow.AddDays(10));

        await service.IngestEventAsync(request);
        await service.IngestEventAsync(request);

        var count = await context.EventEntries.CountAsync(e => e.FeedEntryId == feedEntryId);
        count.Should().Be(1);
    }

    private static string ComputeHash(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
