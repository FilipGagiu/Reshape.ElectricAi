using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddSingleton<IOpenAiClient, OpenAiClient>();
        return services;
    }
}
