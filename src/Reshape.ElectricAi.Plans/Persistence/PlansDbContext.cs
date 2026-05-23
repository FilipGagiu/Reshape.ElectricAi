using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence;

public class PlansDbContext(DbContextOptions<PlansDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<UserPreferenceGenre> UserPreferenceGenres => Set<UserPreferenceGenre>();
    public DbSet<UserPreferenceFoodRestriction> UserPreferenceFoodRestrictions => Set<UserPreferenceFoodRestriction>();
    public DbSet<UserPreferenceActivity> UserPreferenceActivities => Set<UserPreferenceActivity>();
    public DbSet<UserPreferenceArtist> UserPreferenceArtists => Set<UserPreferenceArtist>();
    public DbSet<UserPreferenceCuisine> UserPreferenceCuisines => Set<UserPreferenceCuisine>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupPreferences> GroupPreferences => Set<GroupPreferences>();
    public DbSet<GroupPreferenceGenre> GroupPreferenceGenres => Set<GroupPreferenceGenre>();
    public DbSet<GroupPreferenceFoodRestriction> GroupPreferenceFoodRestrictions => Set<GroupPreferenceFoodRestriction>();
    public DbSet<GroupPreferenceActivity> GroupPreferenceActivities => Set<GroupPreferenceActivity>();
    public DbSet<GroupPreferenceArtist> GroupPreferenceArtists => Set<GroupPreferenceArtist>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("plans");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlansDbContext).Assembly);
    }
}
