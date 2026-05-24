using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Reshape.ElectricAi.AiChat;
using Reshape.ElectricAi.AiChat.Persistence;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.LiveFeed;
using Reshape.ElectricAi.LiveFeed.Persistence;
using Reshape.ElectricAi.Plans;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.VectorDb;
using Reshape.ElectricAi.VectorDb.Persistence;
using Reshape.ElectricAi.VectorDb.Services;
using Reshape.ElectricAi.Presentation.Filters;
using Reshape.ElectricAi.Presentation.Middleware;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

LoadSecretsJson(builder.Configuration, Directory.GetCurrentDirectory());
// Re-add env vars last so they override secrets.json (LoadSecretsJson runs after
// WebApplication.CreateBuilder which already added env vars; without this re-add,
// secrets.json would shadow test fixtures that inject config via environment).
builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services.AddPlansModule(builder.Configuration);
builder.Services.AddVectorDbModule(builder.Configuration);
builder.Services.AddLiveFeedModule(builder.Configuration);
builder.Services.AddAiChatModule(builder.Configuration);


builder.Services.AddScoped<FluentValidationFilter>();
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<FluentValidationFilter>();
    })
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = context =>
    {
        // Raw ModelState messages from System.Text.Json model binding can leak internal
        // type names, JSON paths, and line/byte offsets. Never expose them to clients.
        // Log them server-side for diagnostics, return a generic envelope.
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Reshape.ElectricAi.Presentation.ModelBinding");

        if (logger.IsEnabled(LogLevel.Warning))
        {
            var fieldErrors = context.ModelState
                .Where(kvp => kvp.Value!.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            // Pass the dictionary unserialized so Serilog can destructure (@) into
            // structured fields downstream (Seq/ELK indexing). Source-gen LoggerMessage
            // honors the @ operator in the template.
            ModelBindingLog.LogFailure(
                logger,
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                fieldErrors);
        }

        return new BadRequestObjectResult(
            ErrorEnvelope.Simple("invalid-request", "Invalid request body."));
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<AuthOptions>((jwt, auth) =>
    {
        jwt.MapInboundClaims = false;
        jwt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = auth.Issuer,
            ValidAudience = auth.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(JwtSigningKey.Decode(auth.JwtSigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
        jwt.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (context.Response.HasStarted)
                {
                    return;
                }
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                var code = context.AuthenticateFailure switch
                {
                    SecurityTokenExpiredException => "token-expired",
                    SecurityTokenInvalidIssuerException => "invalid-token",
                    SecurityTokenInvalidAudienceException => "invalid-token",
                    SecurityTokenInvalidSignatureException => "invalid-token",
                    null => "missing-token",
                    _ => "invalid-token"
                };
                await context.Response.WriteAsJsonAsync(ErrorEnvelope.Simple(code, "Authentication is required."));
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(ErrorEnvelope.Simple("forbidden", "Insufficient permissions."));
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.UseSerilogRequestLogging();

using var scope = app.Services.CreateScope();
var plansDb = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
await plansDb.Database.MigrateAsync();
var vectorDb = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
await vectorDb.Database.MigrateAsync();
var feedDb = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
await feedDb.Database.MigrateAsync();
var chatDb = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
await chatDb.Database.MigrateAsync();

// if (app.Environment.IsDevelopment())
// {
    // var seeder = scope.ServiceProvider.GetRequiredService<EcDataSeeder>();
    // var dataRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "data"));
    // await seeder.SeedAsync(dataRoot);
// }

app.UseSwagger();
app.MapScalarApiReference(options =>
    options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json"));

app.UseMiddleware<ExceptionHandlerMiddleware>();

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

static void LoadSecretsJson(ConfigurationManager configuration, string startDir)
{
    var dir = new DirectoryInfo(startDir);
    while (dir is not null)
    {
        var path = Path.Combine(dir.FullName, "secrets.json");
        if (File.Exists(path))
        {
            configuration.AddJsonFile(path, optional: false, reloadOnChange: false);
            return;
        }
        dir = dir.Parent;
    }
}

public partial class Program;

internal static partial class ModelBindingLog
{
    // Source-gen LoggerMessage. Matches the convention used by ExceptionHandlerMiddleware
    // and satisfies CA1848 under TreatWarningsAsErrors. The serialized field-error blob is a
    // JSON string built by the caller; never returned to the client.
    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Model binding failed for {Method} {Path}: {@FieldErrors}")]
    public static partial void LogFailure(
        Microsoft.Extensions.Logging.ILogger logger,
        string method,
        string path,
        IReadOnlyDictionary<string, string[]> fieldErrors);
}
