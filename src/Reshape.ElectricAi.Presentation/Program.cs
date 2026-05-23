using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Reshape.ElectricAi.AiChat;
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
        var details = context.ModelState
            .Where(kvp => kvp.Value!.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        return new BadRequestObjectResult(
            ErrorEnvelope.WithDetails("validation-failed", "One or more validation errors occurred.", details));
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
    var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowed)
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
