using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reshape.ElectricAi.AiChat.Configuration;
using Reshape.ElectricAi.AiChat.Services;
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
            .Validate(o => o.Models.Count > 0, "At least one OpenAi:Models entry is required (e.g. OpenAi:Models:gpt-4o-mini).")
            .ValidateOnStart();

        services.AddOptions<ConversationOptions>()
            .Bind(configuration.GetSection(ConversationOptions.SectionName));

        services.AddSingleton<IOpenAiClient, OpenAiClient>();
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
