using Microsoft.EntityFrameworkCore;
using Pgvector;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.VectorDb.Entities;
using Reshape.ElectricAi.VectorDb.Persistence;
using Reshape.ElectricAi.VectorDb.Services;

namespace Reshape.ElectricAi.VectorDb.Tests;

[Collection("VectorDb")]
public sealed class TopArtistsServiceTests(VectorDbFixture fixture)
{
    private static Vector ZeroEmbedding() => new(new float[VectorDbFixture.TestDimensions]);

    // SQL-only filter has no similarity scoring, so prior rows from other tests in the shared
    // VectorDbFixture container would leak into results. Reset both tables per test.
    private static async Task ResetAsync(VectorDbContext ctx)
    {
        await ctx.DocumentChunks.ExecuteDeleteAsync();
        await ctx.Documents.ExecuteDeleteAsync();
    }

    private static async Task SeedChunkAsync(VectorDbContext ctx, string title, string content)
    {
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Title = title,
            SourceHash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            IngestedUtc = DateTimeOffset.UtcNow,
        };
        ctx.Documents.Add(doc);
        ctx.DocumentChunks.Add(new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = doc.Id,
            Content = content,
            Embedding = ZeroEmbedding(),
            ChunkIndex = 0,
        });
        await ctx.SaveChangesAsync();
    }

    private static string ArtistChunk(string name, string genresLine) => $"""
        Artist: {name}
        Stage: Main Stage at Electric Castle Festival
        Day: Fri | Time: 20:00 – 21:00

        Some description text.
        {genresLine}
        """;

    private static TopArtistsService BuildService(VectorDbContext ctx, params MusicGenre[] userGenres) =>
        new(ctx, new FakeUserPrefsProvider(userGenres));

    [Fact]
    public async Task Returns5_WhenManyChunksMatchUserGenres()
    {
        await using var seedCtx = fixture.CreateContext();
        await ResetAsync(seedCtx);
        for (var i = 0; i < 10; i++)
        {
            await SeedChunkAsync(seedCtx, $"Lineup: ARTIST_{i}_{Guid.NewGuid():N}",
                ArtistChunk($"ARTIST_{i}_{Guid.NewGuid():N}", "Genres: House, Techno"));
        }

        await using var ctx = fixture.CreateContext();
        var result = await BuildService(ctx, MusicGenre.House).GetTopForUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().HaveCount(5);
        result.Should().OnlyContain(name => name.StartsWith("ARTIST_"));
    }

    [Fact]
    public async Task ReturnsEmpty_WhenUserHasNoGenres()
    {
        await using var seedCtx = fixture.CreateContext();
        await ResetAsync(seedCtx);
        await SeedChunkAsync(seedCtx, $"Lineup: SOLO_{Guid.NewGuid():N}",
            ArtistChunk($"SOLO_{Guid.NewGuid():N}", "Genres: House"));

        await using var ctx = fixture.CreateContext();
        var result = await BuildService(ctx).GetTopForUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsEmpty_WhenNoChunksMatchUserGenres()
    {
        await using var seedCtx = fixture.CreateContext();
        await ResetAsync(seedCtx);
        await SeedChunkAsync(seedCtx, $"Lineup: METALHEAD_{Guid.NewGuid():N}",
            ArtistChunk($"METALHEAD_{Guid.NewGuid():N}", "Genres: Metal, Hardcore"));

        await using var ctx = fixture.CreateContext();
        var result = await BuildService(ctx, MusicGenre.House).GetTopForUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractsArtistNameFromContent()
    {
        var name = $"ALDARIS_{Guid.NewGuid():N}";
        await using var seedCtx = fixture.CreateContext();
        await ResetAsync(seedCtx);
        await SeedChunkAsync(seedCtx, $"Lineup: {name}",
            ArtistChunk(name, "Genres: Vinyl Explorer, Deep Grooves"));

        await using var ctx = fixture.CreateContext();
        var result = await BuildService(ctx, MusicGenre.VinylExplorer).GetTopForUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(name);
    }

    [Fact]
    public async Task SplitsCamelCaseGenreEnumToDisplayName()
    {
        var name = $"DRUMNB_{Guid.NewGuid():N}";
        await using var seedCtx = fixture.CreateContext();
        await ResetAsync(seedCtx);
        // Content uses the space-separated display form, mirroring EcDataSeeder.ToDisplayName.
        await SeedChunkAsync(seedCtx, $"Lineup: {name}",
            ArtistChunk(name, "Genres: Drum And Bass"));

        await using var ctx = fixture.CreateContext();
        var result = await BuildService(ctx, MusicGenre.DrumAndBass).GetTopForUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(name);
    }

    [Fact]
    public async Task DedupesByName_CaseInsensitive()
    {
        var rawName = $"DUPER_{Guid.NewGuid():N}";
        await using var seedCtx = fixture.CreateContext();
        await ResetAsync(seedCtx);
        await SeedChunkAsync(seedCtx, $"Lineup: {rawName}-a",
            ArtistChunk(rawName, "Genres: House"));
        await SeedChunkAsync(seedCtx, $"Lineup: {rawName}-b",
            ArtistChunk(rawName.ToLowerInvariant(), "Genres: House"));

        await using var ctx = fixture.CreateContext();
        var result = await BuildService(ctx, MusicGenre.House).GetTopForUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task IgnoresChunksNotStartingWithArtistPrefix()
    {
        var marker = $"FAQ_MARK_{Guid.NewGuid():N}";
        await using var seedCtx = fixture.CreateContext();
        await ResetAsync(seedCtx);
        await SeedChunkAsync(seedCtx, $"FAQ Page", $"FAQ: {marker}\nSomething about House music here.\nGenres: House");

        await using var ctx = fixture.CreateContext();
        var result = await BuildService(ctx, MusicGenre.House).GetTopForUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().NotContain(name => name.Contains(marker));
    }

    [Fact]
    public async Task DoesNotMatch_WhenGenreOnlyAppearsInDescriptionNotGenresLine()
    {
        var name = $"POPULAR_{Guid.NewGuid():N}";
        await using var seedCtx = fixture.CreateContext();
        await ResetAsync(seedCtx);
        // "Pop" appears only in description ("Popular"), not in any Genres: line.
        await SeedChunkAsync(seedCtx, $"Lineup: {name}", $"""
            Artist: {name}
            Stage: Main Stage at Electric Castle Festival
            Day: Fri | Time: 20:00 – 21:00

            Popular indie act with a rock sound.
            """);

        await using var ctx = fixture.CreateContext();
        var result = await BuildService(ctx, MusicGenre.Pop).GetTopForUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private sealed class FakeUserPrefsProvider(params MusicGenre[] genres) : IUserPrefsProvider
    {
        public Task<UserFeedPrefs> GetPrefsByUserIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult(new UserFeedPrefs(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<MusicGenre>(genres)));
    }
}
