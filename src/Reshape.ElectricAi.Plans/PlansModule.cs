using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Services;

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

        services.AddScoped(typeof(IRepository<>), typeof(PlansRepository<>));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

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

        var keyBytes = TokenService.SigningKeyBytes(options.JwtSigningKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("Auth:JwtSigningKey must be at least 32 bytes (256 bits).");
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
