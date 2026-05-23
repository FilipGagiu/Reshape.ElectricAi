using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Extensions;

internal static class PreferencesMappingExtensions
{
    private const int DimensionCount = 13;

    public static PreferencesDto ToDto(this UserPreferences? entity)
    {
        if (entity is null)
        {
            return new PreferencesDto(
                Name: null,
                Origin: null,
                Crew: null,
                VibeTags: [],
                MusicGenres: [],
                MustSeeArtists: [],
                FoodRestrictions: [],
                Cuisines: [],
                ActivityInterests: [],
                SuggestedTransport: null,
                SuggestedAccommodation: null,
                TicketType: null,
                AgeGroup: null,
                CompletionPercent: 0,
                UpdatedUtc: default);
        }

        var vibeTags = entity.VibeTags.Select(v => v.Value).ToArray();
        var musicGenres = entity.Genres.Select(g => g.Genre).ToArray();
        var mustSeeArtists = entity.Artists.Select(a => a.ArtistName).ToArray();
        var foodRestrictions = entity.FoodRestrictions.Select(f => f.Restriction).ToArray();
        var cuisines = entity.Cuisines.Select(c => c.Cuisine).ToArray();
        var activities = entity.Activities.Select(a => a.Activity).ToArray();

        var crew = entity.CrewKind is null ? null : new CrewDto(entity.CrewKind.Value, entity.CrewEstimatedSize);
        var transport = entity.Transport is null ? null : new TransportSuggestionDto(entity.Transport.Value, entity.TransportNote);
        var accommodation = entity.Accommodation is null ? null : new AccommodationSuggestionDto(entity.Accommodation.Value, entity.AccommodationNote);

        var filled = 0;
        if (!string.IsNullOrEmpty(entity.Name)) filled++;
        if (!string.IsNullOrEmpty(entity.Origin)) filled++;
        if (entity.CrewKind.HasValue) filled++;
        if (entity.TicketType.HasValue) filled++;
        if (entity.AgeGroup.HasValue) filled++;
        if (entity.Transport.HasValue) filled++;
        if (entity.Accommodation.HasValue) filled++;
        if (vibeTags.Length > 0) filled++;
        if (musicGenres.Length > 0) filled++;
        if (mustSeeArtists.Length > 0) filled++;
        if (foodRestrictions.Length > 0) filled++;
        if (cuisines.Length > 0) filled++;
        if (activities.Length > 0) filled++;

        var completionPercent = filled * 100 / DimensionCount;

        return new PreferencesDto(
            entity.Name,
            entity.Origin,
            crew,
            vibeTags,
            musicGenres,
            mustSeeArtists,
            foodRestrictions,
            cuisines,
            activities,
            transport,
            accommodation,
            entity.TicketType,
            entity.AgeGroup,
            completionPercent,
            entity.UpdatedUtc);
    }

    public static void ApplyReplace(this UserPreferences entity, PreferencesReplaceRequest request, DateTime nowUtc)
    {
        entity.Name = NormalizeText(request.Name, maxLength: 80);
        entity.Origin = NormalizeText(request.Origin, maxLength: 120);

        entity.CrewKind = request.Crew?.Kind;
        entity.CrewEstimatedSize = ClampCrewSize(request.Crew?.EstimatedSize);

        entity.TicketType = request.TicketType;
        entity.AgeGroup = request.AgeGroup;

        entity.Transport = request.SuggestedTransport?.Mode;
        entity.TransportNote = NormalizeText(request.SuggestedTransport?.Note, maxLength: 200);

        entity.Accommodation = request.SuggestedAccommodation?.Type;
        entity.AccommodationNote = NormalizeText(request.SuggestedAccommodation?.Note, maxLength: 200);

        entity.UpdatedUtc = nowUtc;

        ReplaceVibeTags(entity, request.VibeTags ?? []);
        ReplaceGenres(entity, request.MusicGenres ?? []);
        ReplaceArtists(entity, request.MustSeeArtists ?? []);
        ReplaceFoodRestrictions(entity, request.FoodRestrictions ?? []);
        ReplaceCuisines(entity, request.Cuisines ?? []);
        ReplaceActivities(entity, request.ActivityInterests ?? []);
    }

    public static void ApplyExtracted(this UserPreferences entity, AiExtractedPreferences extracted, DateTime nowUtc)
    {
        entity.Name = NormalizeText(extracted.Name, maxLength: 80);
        entity.Origin = NormalizeText(extracted.Origin, maxLength: 120);

        entity.CrewKind = extracted.Crew?.Kind;
        entity.CrewEstimatedSize = ClampCrewSize(extracted.Crew?.EstimatedSize);

        entity.TicketType = extracted.TicketType;
        entity.AgeGroup = extracted.AgeGroup;

        entity.Transport = extracted.SuggestedTransport?.Mode;
        entity.TransportNote = NormalizeText(extracted.SuggestedTransport?.Note, maxLength: 200);

        entity.Accommodation = extracted.SuggestedAccommodation?.Type;
        entity.AccommodationNote = NormalizeText(extracted.SuggestedAccommodation?.Note, maxLength: 200);

        entity.UpdatedUtc = nowUtc;

        ReplaceVibeTags(entity, extracted.VibeTags ?? []);
        ReplaceGenres(entity, extracted.MusicGenres ?? []);
        ReplaceArtists(entity, extracted.MustSeeArtists ?? []);
        ReplaceFoodRestrictions(entity, extracted.FoodRestrictions ?? []);
        ReplaceCuisines(entity, extracted.Cuisines ?? []);
        ReplaceActivities(entity, extracted.ActivityInterests ?? []);
    }

    public static void ApplyPatch(this UserPreferences entity, PreferencesPatchRequest request, DateTime nowUtc)
    {
        if (request.Name is not null) entity.Name = NormalizeText(request.Name, maxLength: 80);
        if (request.Origin is not null) entity.Origin = NormalizeText(request.Origin, maxLength: 120);

        if (request.Crew is not null)
        {
            entity.CrewKind = request.Crew.Kind;
            entity.CrewEstimatedSize = ClampCrewSize(request.Crew.EstimatedSize);
        }

        if (request.TicketType is not null) entity.TicketType = request.TicketType;
        if (request.AgeGroup is not null) entity.AgeGroup = request.AgeGroup;

        if (request.SuggestedTransport is not null)
        {
            entity.Transport = request.SuggestedTransport.Mode;
            entity.TransportNote = NormalizeText(request.SuggestedTransport.Note, maxLength: 200);
        }

        if (request.SuggestedAccommodation is not null)
        {
            entity.Accommodation = request.SuggestedAccommodation.Type;
            entity.AccommodationNote = NormalizeText(request.SuggestedAccommodation.Note, maxLength: 200);
        }

        if (request.VibeTags is not null) ReplaceVibeTags(entity, request.VibeTags);
        if (request.MusicGenres is not null) ReplaceGenres(entity, request.MusicGenres);
        if (request.MustSeeArtists is not null) ReplaceArtists(entity, request.MustSeeArtists);
        if (request.FoodRestrictions is not null) ReplaceFoodRestrictions(entity, request.FoodRestrictions);
        if (request.Cuisines is not null) ReplaceCuisines(entity, request.Cuisines);
        if (request.ActivityInterests is not null) ReplaceActivities(entity, request.ActivityInterests);

        entity.UpdatedUtc = nowUtc;
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        if (trimmed.Length == 0) return null;
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static short? ClampCrewSize(int? raw)
    {
        if (raw is null) return null;
        if (raw < 1) return 1;
        if (raw > short.MaxValue) return short.MaxValue;
        return (short)raw;
    }

    private static void ReplaceVibeTags(UserPreferences entity, IReadOnlyList<string> tags)
    {
        entity.VibeTags.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in tags)
        {
            var value = NormalizeText(raw, maxLength: 60);
            if (value is null) continue;
            if (!seen.Add(value)) continue;
            entity.VibeTags.Add(new UserPreferenceVibeTag { UserId = entity.UserId, Value = value });
        }
    }

    private static void ReplaceGenres(UserPreferences entity, IReadOnlyList<MusicGenre> genres)
    {
        entity.Genres.Clear();
        foreach (var g in genres.Distinct())
        {
            entity.Genres.Add(new UserPreferenceGenre { UserId = entity.UserId, Genre = g });
        }
    }

    private static void ReplaceFoodRestrictions(UserPreferences entity, IReadOnlyList<FoodRestriction> restrictions)
    {
        entity.FoodRestrictions.Clear();
        foreach (var r in restrictions.Distinct())
        {
            entity.FoodRestrictions.Add(new UserPreferenceFoodRestriction { UserId = entity.UserId, Restriction = r });
        }
    }

    private static void ReplaceActivities(UserPreferences entity, IReadOnlyList<ActivityType> activities)
    {
        entity.Activities.Clear();
        foreach (var a in activities.Distinct())
        {
            entity.Activities.Add(new UserPreferenceActivity { UserId = entity.UserId, Activity = a });
        }
    }

    private static void ReplaceArtists(UserPreferences entity, IReadOnlyList<string> artists)
    {
        entity.Artists.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in artists)
        {
            var name = NormalizeText(raw, maxLength: 200);
            if (name is null) continue;
            if (!seen.Add(name)) continue;
            entity.Artists.Add(new UserPreferenceArtist { UserId = entity.UserId, ArtistName = name });
        }
    }

    private static void ReplaceCuisines(UserPreferences entity, IReadOnlyList<Cuisine> cuisines)
    {
        entity.Cuisines.Clear();
        foreach (var c in cuisines.Distinct())
        {
            entity.Cuisines.Add(new UserPreferenceCuisine { UserId = entity.UserId, Cuisine = c });
        }
    }
}
