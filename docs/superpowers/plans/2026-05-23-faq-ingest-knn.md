# FAQ Ingest & KNN Retrieval Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add two anonymous HTTP endpoints — `POST /api/v1/faq` for ingesting Q&A pairs and `POST /api/v1/faq/search` for KNN semantic search with optional category-based user context filtering.

**Architecture:** The service layer (`IIngestService.IngestQAAsync`, `IVectorSearchService.SearchQuestionsAsync`) is fully implemented. This plan adds only: (1) FluentValidation validators for the request types, (2) a `FaqController` that wires them up, and (3) integration tests. No migrations, no new DTOs, no new service methods.

**Tech Stack:** ASP.NET Core 10 Controllers, FluentValidation 12.1.1, xUnit, FluentAssertions, Testcontainers (PostgreSQL), WebApplicationFactory.

---

## ⚠️ Prerequisite — Package Install (User Action Required)

Before starting Task 1, ask the user to install FluentValidation in the VectorDb project:

```bash
dotnet add src/Reshape.ElectricAi.VectorDb/Reshape.ElectricAi.VectorDb.csproj package FluentValidation --version 12.1.1
```

Wait for the user to confirm the package is installed before proceeding.

---

## File Map

| File | Action |
|---|---|
| `src/Reshape.ElectricAi.VectorDb/Validators/IngestQARequestValidator.cs` | Create |
| `src/Reshape.ElectricAi.VectorDb/Validators/QuestionSearchFilterValidator.cs` | Create |
| `src/Reshape.ElectricAi.VectorDb/VectorDbModule.cs` | Modify — add `RegisterValidators` method |
| `src/Reshape.ElectricAi.Presentation/Controllers/FaqController.cs` | Create |
| `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/FaqApiFactory.cs` | Create |
| `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/FaqFakeEmbeddingService.cs` | Create |
| `tests/Reshape.ElectricAi.Plans.Tests/Integration/Endpoints/FaqControllerTests.cs` | Create |
| `tests/Reshape.ElectricAi.VectorDb.Tests/Unit/Validators/IngestQARequestValidatorTests.cs` | Create |
| `tests/Reshape.ElectricAi.VectorDb.Tests/Unit/Validators/QuestionSearchFilterValidatorTests.cs` | Create |

---

## Task 1: IngestQARequestValidator

**Files:**
- Create: `src/Reshape.ElectricAi.VectorDb/Validators/IngestQARequestValidator.cs`
- Create: `tests/Reshape.ElectricAi.VectorDb.Tests/Unit/Validators/IngestQARequestValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Reshape.ElectricAi.VectorDb.Tests/Unit/Validators/IngestQARequestValidatorTests.cs`:

```csharp
using FluentValidation.TestHelper;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.VectorDb.Validators;

namespace Reshape.ElectricAi.VectorDb.Tests.Unit.Validators;

public sealed class IngestQARequestValidatorTests
{
    private readonly IngestQARequestValidator _sut = new();

    [Fact]
    public void Valid_request_passes()
    {
        var req = new IngestQARequest("Where is the medical tent?", [new IngestAnswerRequest("Near the East entrance.")]);
        _sut.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_questionText_fails_on_QuestionText()
    {
        var req = new IngestQARequest("", [new IngestAnswerRequest("Answer.")]);
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(r => r.QuestionText);
    }

    [Fact]
    public void QuestionText_over_2000_chars_fails()
    {
        var req = new IngestQARequest(new string('x', 2001), [new IngestAnswerRequest("Answer.")]);
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(r => r.QuestionText);
    }

    [Fact]
    public void Empty_answers_list_fails()
    {
        var req = new IngestQARequest("Question?", []);
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(r => r.Answers);
    }

    [Fact]
    public void Empty_answer_text_fails()
    {
        var req = new IngestQARequest("Question?", [new IngestAnswerRequest("")]);
        _sut.TestValidate(req).ShouldHaveAnyValidationError();
    }

    [Fact]
    public void AnswerText_over_4000_chars_fails()
    {
        var req = new IngestQARequest("Question?", [new IngestAnswerRequest(new string('y', 4001))]);
        _sut.TestValidate(req).ShouldHaveAnyValidationError();
    }
}
```

- [ ] **Step 2: Run tests — confirm they fail with "type not found"**

```bash
dotnet test tests/Reshape.ElectricAi.VectorDb.Tests --filter "IngestQARequestValidatorTests" -v minimal
```

Expected: build error — `IngestQARequestValidator` does not exist yet.

- [ ] **Step 3: Create the validator**

Create `src/Reshape.ElectricAi.VectorDb/Validators/IngestQARequestValidator.cs`:

```csharp
using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;

namespace Reshape.ElectricAi.VectorDb.Validators;

public sealed class IngestQARequestValidator : AbstractValidator<IngestQARequest>
{
    public IngestQARequestValidator()
    {
        RuleFor(r => r.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(r => r.Answers).NotEmpty().WithMessage("At least one answer is required.");
        RuleForEach(r => r.Answers).ChildRules(a =>
            a.RuleFor(x => x.AnswerText).NotEmpty().MaximumLength(4000));
    }
}
```

- [ ] **Step 4: Run tests — confirm they pass**

```bash
dotnet test tests/Reshape.ElectricAi.VectorDb.Tests --filter "IngestQARequestValidatorTests" -v minimal
```

Expected: 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Reshape.ElectricAi.VectorDb/Validators/IngestQARequestValidator.cs \
        tests/Reshape.ElectricAi.VectorDb.Tests/Unit/Validators/IngestQARequestValidatorTests.cs
git commit -m "feat(vector-db): add IngestQARequestValidator with unit tests"
```

---

## Task 2: QuestionSearchFilterValidator

**Files:**
- Create: `src/Reshape.ElectricAi.VectorDb/Validators/QuestionSearchFilterValidator.cs`
- Create: `tests/Reshape.ElectricAi.VectorDb.Tests/Unit/Validators/QuestionSearchFilterValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Reshape.ElectricAi.VectorDb.Tests/Unit/Validators/QuestionSearchFilterValidatorTests.cs`:

```csharp
using FluentValidation.TestHelper;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.VectorDb.Validators;

namespace Reshape.ElectricAi.VectorDb.Tests.Unit.Validators;

public sealed class QuestionSearchFilterValidatorTests
{
    private readonly QuestionSearchFilterValidator _sut = new();

    [Fact]
    public void Valid_filter_passes()
    {
        var filter = new QuestionSearchFilter("parking");
        _sut.TestValidate(filter).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_filter_with_userContext_passes()
    {
        var filter = new QuestionSearchFilter(
            "parking",
            new Dictionary<Reshape.ElectricAi.Core.Enums.Category, IReadOnlyList<string>>
            {
                { Reshape.ElectricAi.Core.Enums.Category.Transport, ["Car"] }
            },
            TopK: 10);
        _sut.TestValidate(filter).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_queryText_fails()
    {
        var filter = new QuestionSearchFilter("");
        _sut.TestValidate(filter).ShouldHaveValidationErrorFor(f => f.QueryText);
    }

    [Fact]
    public void QueryText_over_2000_chars_fails()
    {
        var filter = new QuestionSearchFilter(new string('q', 2001));
        _sut.TestValidate(filter).ShouldHaveValidationErrorFor(f => f.QueryText);
    }

    [Fact]
    public void TopK_zero_fails()
    {
        var filter = new QuestionSearchFilter("parking", TopK: 0);
        _sut.TestValidate(filter).ShouldHaveValidationErrorFor(f => f.TopK);
    }

    [Fact]
    public void TopK_51_fails()
    {
        var filter = new QuestionSearchFilter("parking", TopK: 51);
        _sut.TestValidate(filter).ShouldHaveValidationErrorFor(f => f.TopK);
    }

    [Fact]
    public void TopK_1_passes()
    {
        var filter = new QuestionSearchFilter("parking", TopK: 1);
        _sut.TestValidate(filter).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void TopK_50_passes()
    {
        var filter = new QuestionSearchFilter("parking", TopK: 50);
        _sut.TestValidate(filter).ShouldNotHaveAnyValidationErrors();
    }
}
```

- [ ] **Step 2: Run tests — confirm they fail with build error**

```bash
dotnet test tests/Reshape.ElectricAi.VectorDb.Tests --filter "QuestionSearchFilterValidatorTests" -v minimal
```

Expected: build error — `QuestionSearchFilterValidator` does not exist yet.

- [ ] **Step 3: Create the validator**

Create `src/Reshape.ElectricAi.VectorDb/Validators/QuestionSearchFilterValidator.cs`:

```csharp
using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;

namespace Reshape.ElectricAi.VectorDb.Validators;

public sealed class QuestionSearchFilterValidator : AbstractValidator<QuestionSearchFilter>
{
    public QuestionSearchFilterValidator()
    {
        RuleFor(f => f.QueryText).NotEmpty().MaximumLength(2000);
        RuleFor(f => f.TopK).InclusiveBetween(1, 50);
    }
}
```

- [ ] **Step 4: Run tests — confirm they pass**

```bash
dotnet test tests/Reshape.ElectricAi.VectorDb.Tests --filter "QuestionSearchFilterValidatorTests" -v minimal
```

Expected: 8 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Reshape.ElectricAi.VectorDb/Validators/QuestionSearchFilterValidator.cs \
        tests/Reshape.ElectricAi.VectorDb.Tests/Unit/Validators/QuestionSearchFilterValidatorTests.cs
git commit -m "feat(vector-db): add QuestionSearchFilterValidator with unit tests"
```

---

## Task 3: Register Validators in VectorDbModule

**Files:**
- Modify: `src/Reshape.ElectricAi.VectorDb/VectorDbModule.cs`

- [ ] **Step 1: Update VectorDbModule.cs**

Add `using FluentValidation;` and `using Microsoft.Extensions.DependencyInjection.Extensions;` to the top of the file, and add a `RegisterValidators` call at the end of `AddVectorDbModule`:

The full updated file `src/Reshape.ElectricAi.VectorDb/VectorDbModule.cs`:

```csharp
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
        services.AddScoped<IIngestService, IngestService>();
        services.AddScoped<EcDataSeeder>();

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
            EmbeddingModel = section["EmbeddingModel"] ?? "text-embedding-3-small",
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
```

- [ ] **Step 2: Build to confirm no errors**

```bash
dotnet build src/Reshape.ElectricAi.VectorDb/Reshape.ElectricAi.VectorDb.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Reshape.ElectricAi.VectorDb/VectorDbModule.cs
git commit -m "feat(vector-db): register FluentValidation validators in VectorDbModule"
```

---

## Task 4: FaqController

**Files:**
- Create: `src/Reshape.ElectricAi.Presentation/Controllers/FaqController.cs`

- [ ] **Step 1: Create the controller**

Create `src/Reshape.ElectricAi.Presentation/Controllers/FaqController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Presentation.Controllers;

/// <summary>
/// Public FAQ search and ingestion. Both endpoints are anonymous — no JWT required.
/// Ingest is idempotent: posting the same question text twice is a no-op (duplicate skipped by hash).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/faq")]
[Produces("application/json")]
public sealed class FaqController(
    IIngestService ingestService,
    IVectorSearchService vectorSearchService) : ControllerBase
{
    /// <summary>Ingest a new FAQ question and its answers, with optional category tags.</summary>
    /// <remarks>
    /// Idempotent: posting the same <c>questionText</c> (case-sensitive, whitespace-normalized)
    /// a second time is silently ignored — no duplicate is stored.
    ///
    /// Category tags on questions drive KNN filtering: a question tagged
    /// <c>{"Transport": ["Car"]}</c> is returned only when the caller's <c>userContext</c>
    /// includes <c>Transport.Car</c>. Omit <c>questionCategoryValues</c> to make the entry
    /// always visible regardless of caller context.
    /// </remarks>
    /// <param name="request">Question, answers, and optional category values.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="204">Ingested (or already exists — idempotent).</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestAsync(
        [FromBody] IngestQARequest request, CancellationToken cancellationToken)
    {
        await ingestService.IngestQAAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Find the <c>topK</c> most semantically similar FAQ questions to a query.</summary>
    /// <remarks>
    /// Embeds <c>queryText</c> and runs a cosine KNN search over stored question embeddings.
    /// If <c>userContext</c> is provided, only questions whose category tags overlap the context
    /// are considered. Omit or pass <c>null</c> to search all questions.
    ///
    /// Returns an empty list when no matching questions are found — never 404.
    /// </remarks>
    /// <param name="filter">Query text, optional user-context category map, and result count.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Ranked list of matching Q&amp;A pairs (may be empty).</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost("search")]
    [ProducesResponseType(typeof(IReadOnlyList<RetrievedQA>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<RetrievedQA>>> SearchAsync(
        [FromBody] QuestionSearchFilter filter, CancellationToken cancellationToken)
    {
        var results = await vectorSearchService.SearchQuestionsAsync(filter, cancellationToken);
        return Ok(results);
    }
}
```

- [ ] **Step 2: Build the Presentation project**

```bash
dotnet build src/Reshape.ElectricAi.Presentation/Reshape.ElectricAi.Presentation.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Reshape.ElectricAi.Presentation/Controllers/FaqController.cs
git commit -m "feat(presentation): add FaqController with ingest and KNN search endpoints"
```

---

## Task 5: Integration Tests

**Files:**
- Create: `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/FaqFakeEmbeddingService.cs`
- Create: `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/FaqApiFactory.cs`
- Create: `tests/Reshape.ElectricAi.Plans.Tests/Integration/Endpoints/FaqControllerTests.cs`

- [ ] **Step 1: Create the fake embedding service**

Create `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/FaqFakeEmbeddingService.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

internal sealed class FaqFakeEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 1536;

    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text, CancellationToken cancellationToken = default)
        => Task.FromResult(GenerateVector(text));

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(
            texts.Select(GenerateVector).ToList());

    private static ReadOnlyMemory<float> GenerateVector(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var seed = BitConverter.ToInt32(hash, 0);
        var rng = new Random(seed);
        var floats = new float[Dimensions];
        for (var i = 0; i < Dimensions; i++)
            floats[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        var magnitude = MathF.Sqrt(floats.Sum(f => f * f));
        for (var i = 0; i < Dimensions; i++)
            floats[i] /= magnitude;
        return floats;
    }
}
```

- [ ] **Step 2: Create the API factory**

Create `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/FaqApiFactory.cs`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

public sealed class FaqApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", postgres.ConnectionString);
        Environment.SetEnvironmentVariable("Auth__JwtSigningKey", AuthApiFactory.TestSigningKey);
        Environment.SetEnvironmentVariable("Auth__Issuer", "reshape-electric-ai");
        Environment.SetEnvironmentVariable("Auth__Audience", "reshape-electric-ai-api");
        Environment.SetEnvironmentVariable("Auth__AccessTokenMinutes", "15");
        Environment.SetEnvironmentVariable("Auth__RefreshTokenDays", "7");
        Environment.SetEnvironmentVariable("OpenAi__ApiKey", "dummy-key-not-used-in-tests");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmbeddingService));
            if (descriptor is not null)
                services.Remove(descriptor);
            services.AddScoped<IEmbeddingService, FaqFakeEmbeddingService>();
        });
    }
}
```

- [ ] **Step 3: Write the failing integration tests**

Create `tests/Reshape.ElectricAi.Plans.Tests/Integration/Endpoints/FaqControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class FaqControllerTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private FaqApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new FaqApiFactory(postgres);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // --- Ingest ---

    [Fact]
    public async Task Ingest_ValidRequest_Returns204()
    {
        var request = new IngestQARequest(
            $"Test question {Guid.NewGuid()}",
            [new IngestAnswerRequest("Test answer.")]);

        var response = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Ingest_DuplicateQuestion_Returns204Idempotent()
    {
        var questionText = $"Duplicate question {Guid.NewGuid()}";
        var request = new IngestQARequest(questionText, [new IngestAnswerRequest("Answer.")]);

        var first = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);
        var second = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Ingest_EmptyQuestionText_Returns400()
    {
        var request = new IngestQARequest("", [new IngestAnswerRequest("Answer.")]);

        var response = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ingest_EmptyAnswersList_Returns400()
    {
        var request = new IngestQARequest($"Valid question {Guid.NewGuid()}", []);

        var response = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ingest_WithCategoryValues_Returns204()
    {
        var request = new IngestQARequest(
            $"Where can I park? {Guid.NewGuid()}",
            [new IngestAnswerRequest("Use lot B.")],
            QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
            {
                { Category.Transport, ["Car"] }
            });

        var response = await _client.PostAsJsonAsync("/api/v1/faq", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // --- Search ---

    [Fact]
    public async Task Search_ValidFilter_Returns200WithList()
    {
        var questionText = $"What time do gates open? {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(questionText, [new IngestAnswerRequest("Gates open at 14:00.")]),
            JsonOptions);

        var filter = new QuestionSearchFilter(questionText, TopK: 1);
        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<RetrievedQA>>(JsonOptions);
        results.Should().NotBeNull();
        results!.Should().NotBeEmpty();
        results[0].QuestionText.Should().Be(questionText);
    }

    [Fact]
    public async Task Search_NoUserContext_ReturnsAllCategories()
    {
        var tag = Guid.NewGuid().ToString("N");
        var transportQ = $"Transport question {tag}";
        var musicQ = $"Music question {tag}";

        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(transportQ, [new IngestAnswerRequest("Transport answer.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Transport, ["Car"] } }),
            JsonOptions);
        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(musicQ, [new IngestAnswerRequest("Music answer.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Music, ["Rock"] } }),
            JsonOptions);

        var filter = new QuestionSearchFilter(tag, UserContext: null, TopK: 10);
        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<RetrievedQA>>(JsonOptions);
        results.Should().NotBeNull();
        var texts = results!.Select(r => r.QuestionText).ToList();
        texts.Should().Contain(transportQ);
        texts.Should().Contain(musicQ);
    }

    [Fact]
    public async Task Search_WithUserContext_FiltersToMatchingCategory()
    {
        var tag = Guid.NewGuid().ToString("N");
        var transportQ = $"Car parking details {tag}";
        var foodQ = $"Food stall location {tag}";

        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(transportQ, [new IngestAnswerRequest("Lot B north side.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Transport, ["Car"] } }),
            JsonOptions);
        await _client.PostAsJsonAsync("/api/v1/faq",
            new IngestQARequest(foodQ, [new IngestAnswerRequest("Near the east gate.")],
                QuestionCategoryValues: new Dictionary<Category, IReadOnlyList<string>>
                    { { Category.Food, ["Vegan"] } }),
            JsonOptions);

        var filter = new QuestionSearchFilter(
            tag,
            UserContext: new Dictionary<Category, IReadOnlyList<string>>
                { { Category.Transport, ["Car"] } },
            TopK: 10);
        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<RetrievedQA>>(JsonOptions);
        results.Should().NotBeNull();
        results!.Should().AllSatisfy(r =>
            r.QuestionText.Should().NotBe(foodQ));
    }

    [Fact]
    public async Task Search_EmptyQueryText_Returns400()
    {
        var filter = new QuestionSearchFilter("");

        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_TopKOutOfRange_Returns400()
    {
        var filter = new QuestionSearchFilter("some query", TopK: 0);

        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_NoResults_Returns200WithEmptyList()
    {
        var filter = new QuestionSearchFilter($"completely unrelated {Guid.NewGuid()}", TopK: 1);

        var response = await _client.PostAsJsonAsync("/api/v1/faq/search", filter, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<RetrievedQA>>(JsonOptions);
        results.Should().NotBeNull();
    }
}
```

- [ ] **Step 4: Run integration tests — confirm they fail (controller doesn't exist yet... but we already added it in Task 4)**

At this point `FaqController` from Task 4 is already in place. Run the tests:

```bash
dotnet test tests/Reshape.ElectricAi.Plans.Tests --filter "FaqControllerTests" -v minimal
```

Expected: all tests pass. If any fail, inspect output — likely a DI or config issue in `FaqApiFactory`.

- [ ] **Step 5: Run the full test suite**

```bash
dotnet test
```

Expected: all existing tests still pass, new FAQ tests pass.

- [ ] **Step 6: Commit**

```bash
git add tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/FaqFakeEmbeddingService.cs \
        tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/FaqApiFactory.cs \
        tests/Reshape.ElectricAi.Plans.Tests/Integration/Endpoints/FaqControllerTests.cs
git commit -m "test(presentation): add FaqController integration tests with fake embedding service"
```

---

## Self-Review

**Spec coverage:**
- ✅ `POST /api/v1/faq` anonymous ingest → Task 4
- ✅ `POST /api/v1/faq/search` anonymous search → Task 4
- ✅ `IngestQARequest` used directly as request body → Task 4 (no new DTO)
- ✅ `QuestionSearchFilter` used directly as request body → Task 4
- ✅ Validation — `IngestQARequestValidator` → Task 1; `QuestionSearchFilterValidator` → Task 2
- ✅ Validator registration in `VectorDbModule` → Task 3
- ✅ `userContext: null` → no category filter → `VectorSearchService` already handles this
- ✅ Idempotent ingest (silent duplicate skip) → tested in Task 5
- ✅ 204 on ingest, 200+list on search, 400 on validation fail → all tested in Task 5
- ✅ No migrations, no new DTOs → confirmed across all tasks

**Placeholder scan:** None found.

**Type consistency:** `IngestQARequest`, `IngestAnswerRequest`, `QuestionSearchFilter`, `RetrievedQA` — all from `Core.Dtos.VectorSearch`, referenced consistently across Tasks 1–5.
