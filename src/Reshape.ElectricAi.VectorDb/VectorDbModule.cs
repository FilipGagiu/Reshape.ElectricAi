using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using Pgvector.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.VectorDb.Entities;
using Reshape.ElectricAi.VectorDb.Persistence;
using Reshape.ElectricAi.VectorDb.Services;

namespace Reshape.ElectricAi.VectorDb;

public static class VectorDbModule
{
    public static IServiceCollection AddVectorDbModule(this IServiceCollection services, IConfiguration configuration)
    {
        var chatOptions = BuildChatOptions(configuration);
        ValidateChatOptions(chatOptions);
        services.AddSingleton<IOptions<ChatOptions>>(Options.Create(chatOptions));

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<VectorDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "vector");
                npgsql.UseVector();
            }));

        services.AddScoped<IRepository<Document>, VectorRepository<Document>>();
        services.AddScoped<IRepository<DocumentChunk>, VectorRepository<DocumentChunk>>();
        services.AddScoped<IRepository<Question>, VectorRepository<Question>>();
        services.AddScoped<IRepository<EventEntry>, VectorRepository<EventEntry>>();

        var apiKey = configuration["OpenAi:ApiKey"]
            ?? throw new InvalidOperationException(
                "OpenAi:ApiKey is required. Set it via user-secrets in dev, environment variable in prod.");

        services.AddSingleton(new EmbeddingClient(chatOptions.EmbeddingModel, apiKey));
        services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddScoped<IVectorSearchService, VectorSearchService>();
        services.AddScoped<Reshape.ElectricAi.Core.Services.Itinerary.IEventLookupService, EventLookupService>();
        services.AddScoped<ITopArtistsService, TopArtistsService>();
        services.AddScoped<IIngestService, IngestService>();
        services.AddScoped<EcDataSeeder>();
        services.AddSingleton<SeedJobChannel>();
        services.AddHostedService<SeedBackgroundService>();

        services.AddScoped<GenreBackfillService>();
        services.AddSingleton<GenreBackfillJobChannel>();
        services.AddHostedService<GenreBackfillBackgroundService>();

        RegisterValidators(services);

        return services;
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        var validatorInterface = typeof(IValidator<>);
        var registrations = typeof(VectorDbModule).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsClass: true })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface)
                .Select(i => new { Service = i, Implementation = t }));

        foreach (var r in registrations)
            services.TryAddScoped(r.Service, r.Implementation);
    }

    private static ChatOptions BuildChatOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(ChatOptions.SectionName);
        return new ChatOptions
        {
            EmbeddingModel = section["EmbeddingModel"] ?? "text-embedding-3-large",
            EmbeddingDimensions = int.TryParse(section["EmbeddingDimensions"], out var dims) ? dims : 1536,
        };
    }

    private static void ValidateChatOptions(ChatOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.EmbeddingModel))
            throw new InvalidOperationException("Chat:EmbeddingModel is required.");

        if (options.EmbeddingDimensions < 1)
            throw new InvalidOperationException("Chat:EmbeddingDimensions must be a positive integer.");
    }
}
