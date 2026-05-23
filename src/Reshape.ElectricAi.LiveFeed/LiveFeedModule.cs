using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Broadcasting;
using Reshape.ElectricAi.LiveFeed.Entities;
using Reshape.ElectricAi.LiveFeed.Persistence;
using Reshape.ElectricAi.LiveFeed.Services;

namespace Reshape.ElectricAi.LiveFeed;

public static class LiveFeedModule
{
    public static IServiceCollection AddLiveFeedModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<FeedDbContext>(opts =>
            opts.UseNpgsql(connectionString, n =>
                n.MigrationsHistoryTable("__EFMigrationsHistory", "feed")));

        // Close the generic per-entity to avoid shadowing other libs' IRepository<> registrations
        // (e.g. PlansModule registers open-generic IRepository<> -> PlansRepository<>).
        // Open-generic registrations from multiple libs would replace each other -- last wins -- and
        // resolving IRepository<User> would land in FeedRepository<User>, which fails on
        // FeedDbContext.Set<User>(). Per-entity closed registrations don't collide.
        services.AddScoped<IRepository<FeedEntry>, FeedRepository<FeedEntry>>();
        services.AddScoped<IRepository<FeedEntryArtist>, FeedRepository<FeedEntryArtist>>();
        services.AddScoped<IRepository<FeedEntryGenre>, FeedRepository<FeedEntryGenre>>();

        services.AddScoped<IFeedService, FeedService>();
        services.AddSingleton<IFeedBroadcaster, FeedBroadcaster>();
        services.TryAddScoped<IUserPrefsProvider, EmptyUserPrefsProvider>();

        RegisterValidators(services);

        return services;
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        var validatorInterface = typeof(IValidator<>);
        var registrations = typeof(LiveFeedModule).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsClass: true })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface)
                .Select(i => new { Service = i, Implementation = t }));

        foreach (var r in registrations)
            services.TryAddScoped(r.Service, r.Implementation);
    }
}
