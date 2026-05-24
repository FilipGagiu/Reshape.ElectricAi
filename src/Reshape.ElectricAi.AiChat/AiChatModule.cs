using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reshape.ElectricAi.AiChat.Configuration;
using Reshape.ElectricAi.AiChat.Entities;
using Reshape.ElectricAi.AiChat.Persistence;
using Reshape.ElectricAi.AiChat.Services;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.AiChat;

public static class AiChatModule
{
    public static IServiceCollection AddAiChatModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "OpenAi:ApiKey is required (user-secrets in dev, env var in prod).")
            .Validate(o => o.Limits.TimeoutSeconds >= 5, "OpenAi:Limits:TimeoutSeconds must be >= 5.")
            .Validate(o => o.Models.Count > 0, "At least one OpenAi:Models entry is required (e.g. OpenAi:Models:gpt-5-mini).")
            .ValidateOnStart();

        services.AddOptions<AskOptions>()
            .Bind(configuration.GetSection(AskOptions.SectionName));

        services.AddOptions<ConversationOptions>()
            .Bind(configuration.GetSection(ConversationOptions.SectionName))
            .Validate(o => o.UserMessageCap > 0, "Conversation:UserMessageCap must be > 0.")
            .Validate(o => o.MaxMessageChars is > 0 and <= 4000, "Conversation:MaxMessageChars must be in (0, 4000].")
            .Validate(o => o.LockTimeoutSeconds >= 30, "Conversation:LockTimeoutSeconds must be >= 30.")
            .Validate(o => o.TitleMaxChars is > 0 and <= 200, "Conversation:TitleMaxChars must be in (0, 200].")
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<ChatDbContext>(opts =>
            opts.UseNpgsql(connectionString, n =>
                n.MigrationsHistoryTable("__EFMigrationsHistory", "chat")));

        // Per-entity closed registrations to avoid shadowing PlansModule's open-generic IRepository<>.
        services.AddScoped<IRepository<Conversation>, ChatRepository<Conversation>>();
        services.AddScoped<IRepository<ConversationMessage>, ChatRepository<ConversationMessage>>();

        services.AddSingleton<IOpenAiClient, OpenAiClient>();
        services.AddScoped<IAskService, AskService>();
        services.AddScoped<IConversationService, ConversationService>();

        RegisterValidators(services);

        return services;
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        var validatorInterface = typeof(IValidator<>);
        var registrations = typeof(AiChatModule).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsClass: true })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface)
                .Select(i => new { Service = i, Implementation = t }));

        foreach (var r in registrations)
            services.TryAddScoped(r.Service, r.Implementation);
    }
}
