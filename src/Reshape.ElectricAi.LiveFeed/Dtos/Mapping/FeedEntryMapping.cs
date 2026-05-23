using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Dtos.Mapping;

public static class FeedEntryMapping
{
    public static FeedEntryDto ToDto(this FeedEntry entity) =>
        new(
            entity.Id,
            entity.Title,
            entity.Body,
            entity.PrimaryCategory,
            entity.IsGeneral,
            entity.TargetArtists.Select(a => a.ArtistName).ToList(),
            entity.TargetGenres.Select(g => g.Genre).ToList(),
            entity.PublishedUtc,
            entity.UpdatedUtc);

    public static PublishFeedEntryCommand ToCommand(this PublishFeedEntryRequest req) =>
        new(req.Title, req.Body, req.PrimaryCategory, req.IsGeneral, req.TargetArtists, req.TargetGenres);

    public static UpdateFeedEntryCommand ToCommand(this UpdateFeedEntryRequest req) =>
        new(req.Title, req.Body, req.PrimaryCategory, req.IsGeneral, req.TargetArtists, req.TargetGenres);

    public static FeedEntry ToNewEntity(this PublishFeedEntryCommand cmd, Guid organizerId)
    {
        var entry = new FeedEntry
        {
            Id = Guid.NewGuid(),
            Title = cmd.Title,
            Body = cmd.Body,
            PrimaryCategory = cmd.PrimaryCategory,
            IsGeneral = cmd.IsGeneral,
            PublishedByUserId = organizerId,
            PublishedUtc = DateTime.UtcNow,
            TargetArtists = cmd.TargetArtists
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(a => new FeedEntryArtist { ArtistName = a })
                .ToList(),
            TargetGenres = cmd.TargetGenres
                .Distinct()
                .Select(g => new FeedEntryGenre { Genre = g })
                .ToList()
        };
        foreach (var a in entry.TargetArtists) a.FeedEntryId = entry.Id;
        foreach (var g in entry.TargetGenres) g.FeedEntryId = entry.Id;
        return entry;
    }

    public static void ApplyUpdateTo(this UpdateFeedEntryCommand cmd, FeedEntry entity)
    {
        entity.Title = cmd.Title;
        entity.Body = cmd.Body;
        entity.PrimaryCategory = cmd.PrimaryCategory;
        entity.IsGeneral = cmd.IsGeneral;
        entity.UpdatedUtc = DateTime.UtcNow;

        entity.TargetArtists.Clear();
        foreach (var a in cmd.TargetArtists.Distinct(StringComparer.OrdinalIgnoreCase))
            entity.TargetArtists.Add(new FeedEntryArtist { FeedEntryId = entity.Id, ArtistName = a });

        entity.TargetGenres.Clear();
        foreach (var g in cmd.TargetGenres.Distinct())
            entity.TargetGenres.Add(new FeedEntryGenre { FeedEntryId = entity.Id, Genre = g });
    }

    public static IngestEventRequest ToIngestEventRequest(this FeedEntry entry)
    {
        var textRepresentation = $"{entry.Title}\n\n{entry.Body}";

        var tags = new Dictionary<Category, IReadOnlyList<string>>();

        if (entry.TargetGenres.Count > 0)
        {
            tags[Category.Music] = entry.TargetGenres
                .Select(g => g.Genre.ToString())
                .Distinct()
                .ToList();
        }

        if (!tags.ContainsKey(entry.PrimaryCategory))
        {
            tags[entry.PrimaryCategory] = [entry.PrimaryCategory.ToString()];
        }

        return new IngestEventRequest(
            FeedEntryId: entry.Id,
            Title: entry.Title,
            TextRepresentation: textRepresentation,
            EventUtc: entry.PublishedUtc,
            CategoryValues: tags);
    }
}
