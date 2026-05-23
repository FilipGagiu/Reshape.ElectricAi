using Reshape.ElectricAi.Core.Dtos.Groups;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Extensions;

internal static class GroupMappingExtensions
{
    private const int DimensionCount = 9;

    public static GroupDto ToGroupDto(this Group group)
    {
        var members = group.Members
            .OrderBy(m => m.JoinedUtc)
            .Select(m => new GroupMemberDto(m.UserId, m.User?.Email ?? string.Empty, m.JoinedUtc))
            .ToArray();
        return new GroupDto(group.Id, group.Name, group.OwnerUserId, group.CreatedUtc, members);
    }

    public static GroupMemberDto ToMemberDto(this GroupMember member, string email) =>
        new(member.UserId, email, member.JoinedUtc);

    public static GroupPreferencesDto ToGroupPreferencesDto(this GroupPreferences? entity)
    {
        if (entity is null)
        {
            return new GroupPreferencesDto(
                TicketType: null,
                Accommodation: null,
                Transport: null,
                AgeGroup: null,
                MusicGenres: [],
                FoodRestrictions: [],
                Activities: [],
                Artists: [],
                Cuisines: [],
                CompletionPercent: 0,
                UpdatedUtc: default);
        }

        var musicGenres = entity.Genres.Select(g => g.Genre).ToArray();
        var foodRestrictions = entity.FoodRestrictions.Select(f => f.Restriction).ToArray();
        var activities = entity.Activities.Select(a => a.Activity).ToArray();
        var artists = entity.Artists.Select(a => a.ArtistName).ToArray();
        var cuisines = entity.Cuisines.Select(c => c.Cuisine).ToArray();

        var filled = 0;
        if (entity.TicketType is not null) filled++;
        if (entity.Accommodation is not null) filled++;
        if (entity.Transport is not null) filled++;
        if (entity.AgeGroup is not null) filled++;
        if (musicGenres.Length > 0) filled++;
        if (foodRestrictions.Length > 0) filled++;
        if (activities.Length > 0) filled++;
        if (artists.Length > 0) filled++;
        if (cuisines.Length > 0) filled++;

        var completionPercent = filled * 100 / DimensionCount;

        return new GroupPreferencesDto(
            entity.TicketType,
            entity.Accommodation,
            entity.Transport,
            entity.AgeGroup,
            musicGenres,
            foodRestrictions,
            activities,
            artists,
            cuisines,
            completionPercent,
            entity.UpdatedUtc);
    }

    public static void ApplyReplace(this GroupPreferences entity, GroupPreferencesReplaceRequest request, DateTime nowUtc)
    {
        entity.TicketType = request.TicketType;
        entity.Accommodation = request.Accommodation;
        entity.Transport = request.Transport;
        entity.AgeGroup = request.AgeGroup;
        entity.UpdatedUtc = nowUtc;

        ReplaceGenres(entity, request.MusicGenres ?? []);
        ReplaceFoodRestrictions(entity, request.FoodRestrictions ?? []);
        ReplaceActivities(entity, request.Activities ?? []);
        ReplaceArtists(entity, request.Artists ?? []);
        ReplaceCuisines(entity, request.Cuisines ?? []);
    }

    public static void ApplyPatch(this GroupPreferences entity, GroupPreferencesPatchRequest request, DateTime nowUtc)
    {
        if (request.TicketType is not null) entity.TicketType = request.TicketType;
        if (request.Accommodation is not null) entity.Accommodation = request.Accommodation;
        if (request.Transport is not null) entity.Transport = request.Transport;
        if (request.AgeGroup is not null) entity.AgeGroup = request.AgeGroup;

        if (request.MusicGenres is not null) ReplaceGenres(entity, request.MusicGenres);
        if (request.FoodRestrictions is not null) ReplaceFoodRestrictions(entity, request.FoodRestrictions);
        if (request.Activities is not null) ReplaceActivities(entity, request.Activities);
        if (request.Artists is not null) ReplaceArtists(entity, request.Artists);
        if (request.Cuisines is not null) ReplaceCuisines(entity, request.Cuisines);

        entity.UpdatedUtc = nowUtc;
    }

    private static void ReplaceGenres(GroupPreferences entity, IReadOnlyList<MusicGenre> genres)
    {
        entity.Genres.Clear();
        foreach (var g in genres.Distinct())
        {
            entity.Genres.Add(new GroupPreferenceGenre { GroupId = entity.GroupId, Genre = g });
        }
    }

    private static void ReplaceFoodRestrictions(GroupPreferences entity, IReadOnlyList<FoodRestriction> restrictions)
    {
        entity.FoodRestrictions.Clear();
        foreach (var r in restrictions.Distinct())
        {
            entity.FoodRestrictions.Add(new GroupPreferenceFoodRestriction { GroupId = entity.GroupId, Restriction = r });
        }
    }

    private static void ReplaceActivities(GroupPreferences entity, IReadOnlyList<ActivityType> activities)
    {
        entity.Activities.Clear();
        foreach (var a in activities.Distinct())
        {
            entity.Activities.Add(new GroupPreferenceActivity { GroupId = entity.GroupId, Activity = a });
        }
    }

    private static void ReplaceArtists(GroupPreferences entity, IReadOnlyList<string> artists)
    {
        entity.Artists.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in artists)
        {
            var name = (raw ?? string.Empty).Trim();
            if (name.Length == 0) continue;
            if (!seen.Add(name)) continue;
            entity.Artists.Add(new GroupPreferenceArtist { GroupId = entity.GroupId, ArtistName = name });
        }
    }

    private static void ReplaceCuisines(GroupPreferences entity, IReadOnlyList<Cuisine> cuisines)
    {
        entity.Cuisines.Clear();
        foreach (var c in cuisines.Distinct())
        {
            entity.Cuisines.Add(new GroupPreferenceCuisine { GroupId = entity.GroupId, Cuisine = c });
        }
    }
}
