using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Itinerary;
using Reshape.ElectricAi.Plans.Configuration;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Services;
using Reshape.ElectricAi.Plans.Services.Itinerary;
using Reshape.ElectricAi.Plans.Services.Itinerary.Sections;

namespace Reshape.ElectricAi.Plans;

public static class PlansModule
{
    public static IServiceCollection AddPlansModule(this IServiceCollection services, IConfiguration configuration)
    {
        var authOptions = BuildAuthOptions(configuration);
        ValidateAuthOptions(authOptions);
        services.AddSingleton(authOptions);
        services.AddSingleton<IOptions<AuthOptions>>(Options.Create(authOptions));

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<PlansDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "plans")));

        services.AddScoped<IRepository<User>, PlansRepository<User>>();
        services.AddScoped<IRepository<RefreshToken>, PlansRepository<RefreshToken>>();
        services.AddScoped<IRepository<UserPreferences>, PlansRepository<UserPreferences>>();
        services.AddScoped<IRepository<Plan>, PlansRepository<Plan>>();
        services.AddScoped<IRepository<Group>, PlansRepository<Group>>();
        services.AddScoped<IRepository<GroupMember>, PlansRepository<GroupMember>>();
        services.AddScoped<IRepository<UserPreferenceActivity>, PlansRepository<UserPreferenceActivity>>();
        services.AddScoped<IRepository<UserPreferenceArtist>, PlansRepository<UserPreferenceArtist>>();
        services.AddScoped<IRepository<UserPreferenceFoodRestriction>, PlansRepository<UserPreferenceFoodRestriction>>();
        services.AddScoped<IRepository<UserPreferenceGenre>, PlansRepository<UserPreferenceGenre>>();
        services.AddScoped<IRepository<UserPreferenceCuisine>, PlansRepository<UserPreferenceCuisine>>();
        services.AddScoped<IRepository<UserPreferenceVibeTag>, PlansRepository<UserPreferenceVibeTag>>();
        services.AddScoped<IRepository<PushSubscription>, PlansRepository<PushSubscription>>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPreferencesService, PreferencesService>();
        services.AddScoped<IGroupService, GroupService>();

        // Bridge from Plans `UserPreferences` -> LiveFeed targeting prefs. Registered as
        // AddScoped (not TryAdd) so this concrete provider wins over LiveFeed's fallback
        // `EmptyUserPrefsProvider` which uses TryAdd. PlansModule runs before LiveFeedModule
        // in Program.cs, so LiveFeed's TryAdd no-ops on top of this registration.
        services.AddScoped<IUserPrefsProvider, PlansUserPrefsProvider>();

        var pushOptions = BuildPushOptions(configuration);
        services.AddSingleton(pushOptions);
        services.AddSingleton<IOptions<PushOptions>>(Options.Create(pushOptions));
        services.AddScoped<IPushService, PushService>();

        services.AddSingleton<IRateLimiter, InMemorySlidingWindowRateLimiter>();

        services.AddOptions<ItineraryGenerationOptions>()
            .Bind(configuration.GetSection(ItineraryGenerationOptions.SectionName))
            .Validate(o => o.MaxCompletionTokens > 0, "ItineraryGeneration:MaxCompletionTokens must be > 0.")
            .Validate(o => o.RateLimit.PerHour >= 1, "ItineraryGeneration:RateLimit:PerHour must be >= 1.")
            .Validate(o => o.PrefsRateLimit.PerHour >= 1, "ItineraryGeneration:PrefsRateLimit:PerHour must be >= 1.")
            .ValidateOnStart();

        services.AddScoped<IPreferencesExtractor, PreferencesExtractor>();
        services.AddScoped<IItineraryRefiner, ItineraryRefiner>();
        services.AddScoped<IItineraryBuilder, ItineraryBuilder>();
        services.AddScoped<IItineraryService, ItineraryService>();

        services.AddScoped<IItinerarySection, GreetingSection>();
        services.AddScoped<IItinerarySection, TransportSection>();
        services.AddScoped<IItinerarySection, AccommodationSection>();
        services.AddScoped<IItinerarySection, VibeActivitiesSection>();
        services.AddScoped<IItinerarySection, FoodSection>();
        services.AddScoped<IItinerarySection, TopArtistsSection>();

        RegisterValidators(services);

        return services;
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        var validatorInterface = typeof(IValidator<>);
        var validatorRegistrations = typeof(PlansModule).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsClass: true })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface)
                .Select(i => new { Service = i, Implementation = type }));

        foreach (var registration in validatorRegistrations)
        {
            services.TryAddScoped(registration.Service, registration.Implementation);
        }
    }

    private static PushOptions BuildPushOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(PushOptions.SectionName);
        return new PushOptions
        {
            VapidPublicKey = section["VapidPublicKey"] ?? string.Empty,
            VapidPrivateKey = section["VapidPrivateKey"] ?? string.Empty,
            Subject = section["Subject"] ?? string.Empty
        };
    }

    private static AuthOptions BuildAuthOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(AuthOptions.SectionName);
        return new AuthOptions
        {
            Issuer = section["Issuer"] ?? "reshape-electric-ai",
            Audience = section["Audience"] ?? "reshape-electric-ai-api",
            JwtSigningKey = section["JwtSigningKey"] ?? string.Empty,
            AccessTokenMinutes = int.TryParse(section["AccessTokenMinutes"], out var accessMinutes) ? accessMinutes : 15,
            RefreshTokenDays = int.TryParse(section["RefreshTokenDays"], out var refreshDays) ? refreshDays : 7,
            SingleSession = bool.TryParse(section["SingleSession"], out var singleSession) && singleSession
        };
    }

    private static void ValidateAuthOptions(AuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.JwtSigningKey))
        {
            throw new InvalidOperationException("Auth:JwtSigningKey is required (user-secrets in dev, env var in prod).");
        }

        if (JwtSigningKey.LooksLikeBase64ButTooShort(options.JwtSigningKey))
        {
            throw new InvalidOperationException(
                "Auth:JwtSigningKey looks like base64 but decodes to fewer than 32 bytes. " +
                "Supply a longer base64 key (e.g. `openssl rand -base64 48`) or a raw UTF-8 passphrase >= 32 chars.");
        }

        var keyBytes = JwtSigningKey.Decode(options.JwtSigningKey);
        if (keyBytes.Length < JwtSigningKey.MinimumBytes)
        {
            throw new InvalidOperationException($"Auth:JwtSigningKey must be at least {JwtSigningKey.MinimumBytes} bytes (256 bits).");
        }

        if (options.AccessTokenMinutes < 1)
        {
            throw new InvalidOperationException("Auth:AccessTokenMinutes must be >= 1.");
        }

        if (options.RefreshTokenDays < 1)
        {
            throw new InvalidOperationException("Auth:RefreshTokenDays must be >= 1.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("Auth:Issuer and Auth:Audience are required.");
        }
    }
}
