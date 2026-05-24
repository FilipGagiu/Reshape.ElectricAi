# Seed Fire-and-Forget Design

**Date:** 2026-05-24
**Branch:** feature/make-seed-fire-and-forget

## Problem

`POST /api/v1/admin/seed` awaits `EcDataSeeder.SeedAsync` synchronously on the HTTP request. Seeding is slow (every chunk roundtrips to OpenAI for embeddings). Reverse proxies (nginx/Caddy) kill connections at ~60-120s. The request `CancellationToken` is passed down — if the connection drops, seeding is cancelled mid-ingestion, leaving the DB partially seeded.

## Solution

Return `202 Accepted` immediately. Run seeding in a long-lived `BackgroundService` decoupled from the request lifetime. Reject concurrent seed requests with `409 Conflict` via the standard error envelope.

**Note:** initial design considered `Task.Run` for the background dispatch. **CODE.md line 237** explicitly forbids fire-and-forget `Task.Run` for long-running background work — it mandates `IHostedService` / `BackgroundService`. Design corrected to use a hosted service consuming a bounded channel.

## Architecture

### `SeedJobChannel` (new, singleton)

Location: `src/Reshape.ElectricAi.VectorDb/Services/SeedJobChannel.cs`

Responsibilities:
- Wraps a `Channel<string>` created with `BoundedChannelOptions(1) { FullMode = DropWrite }` so only one seed path can be enqueued at a time.
- Holds an `Interlocked` `_running` flag (0 = idle, 1 = queued-or-running).
- Exposes:
  - `bool TryEnqueue(string dataPath)` — atomically transitions flag 0→1 and writes the path to the channel. Returns `false` if a seed is already in flight.
  - `IAsyncEnumerable<string> ReadAllAsync(CancellationToken)` — consumer-facing reader.
  - `internal void MarkComplete()` — resets the flag once the background worker finishes (success or failure).

### `SeedBackgroundService : BackgroundService` (new, hosted)

Location: `src/Reshape.ElectricAi.VectorDb/Services/SeedBackgroundService.cs`

Responsibilities:
- Injects `SeedJobChannel`, `IServiceScopeFactory`, `ILogger<SeedBackgroundService>`.
- `ExecuteAsync(stoppingToken)` loops over `queue.ReadAllAsync(stoppingToken)`.
- For each `dataPath`:
  - Creates an `IServiceScope` (required — `EcDataSeeder` and `VectorDbContext` are Scoped).
  - Resolves `EcDataSeeder` and `await`s `SeedAsync(dataPath, stoppingToken)`.
  - `try`/`catch`/`finally` with `LoggerMessage.Define`-style structured logs (`Seed started`, `Seed completed`, `Seed failed`). `MarkComplete()` always runs in `finally`.
  - `OperationCanceledException` is filtered out of the error log — host shutdown is normal, not a failure.

### `AdminController` changes

- Constructor: drop `EcDataSeeder`, inject `SeedJobChannel`.
- Action `Seed`:
  - Synchronous (no async work to do).
  - `queue.TryEnqueue(request.DataPath) == false` → `throw new ConflictException("seed-in-progress", ...)` → middleware maps to `409` + standard error envelope.
  - Otherwise → `Accepted()` → `202`.
- `[ProducesResponseType]` attributes updated: `202` replaces `204`; `409` added.

### Registration

`VectorDbModule.cs`:

```csharp
services.AddSingleton<SeedJobChannel>();
services.AddHostedService<SeedBackgroundService>();
```

`Microsoft.Extensions.Hosting.Abstractions` is already transitive via EF Core — no new package required.

## Error Handling

- Exceptions inside the background worker → caught (except `OperationCanceledException`), logged via `ILogger<SeedBackgroundService>` at Error with the full exception, flag released in `finally` so the next seed can run.
- Partial seed on failure: existing hash-based idempotency (`vector.documents.ContentHash`) means a retry skips already-ingested chunks and resumes.
- Host shutdown mid-seed: `stoppingToken` propagates into `EcDataSeeder.SeedAsync`. On restart, the next seed call resumes via the same hash idempotency.

## What Does Not Change

- `EcDataSeeder.SeedAsync` — no changes.
- Validation (`SeedDataRequestValidator`) — runs before the queue is touched.
- Auth (`[Authorize(Roles = "Organizer")]`) — unchanged.
- Standard error envelope — `ConflictException` flows through `ExceptionHandlerMiddleware`.
