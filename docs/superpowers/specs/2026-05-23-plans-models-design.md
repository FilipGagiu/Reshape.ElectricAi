# Plans-Zone Models + Initial Migration — Design Spec

**Date:** 2026-05-23
**Owner:** Dev 1 (`Reshape.ElectricAi.Plans` + `Reshape.ElectricAi.Core`)
**Status:** Approved — ready for implementation
**Scope:** Models + initial EF migration only. No services, controllers, DTOs, validators, DI wiring, tests, or seeding.

---

## 1. Goal

Lay down the persistence layer for the Plans lib of the Electric Castle AI Builder Challenge backend: entities, `PlansDbContext`, and the first migration that creates the `plans` schema in Postgres.

This unblocks parallel work on the AiChat and LiveFeed libs because their cross-context `UserId` references become stable.

---

## 2. Project + assembly placement

### `Reshape.ElectricAi.Core` (this task adds)
```
Core/
├── Domain/
│   └── ICategorizable.cs
└── Enums/
    ├── Category.cs
    ├── UserRole.cs
    ├── TicketType.cs
    ├── TransportMode.cs
    ├── Accommodation.cs
    ├── MusicGenre.cs
    ├── FoodRestriction.cs
    ├── ActivityType.cs
    ├── AgeGroup.cs
    ├── PlanScope.cs
    └── PlanState.cs
```

No EF, no Npgsql dependency in Core. Pure abstractions per CODE.md.

### `Reshape.ElectricAi.Plans` (this task adds)
```
Plans/
├── Entities/
│   ├── User.cs
│   ├── RefreshToken.cs
│   ├── UserPreferences.cs
│   ├── UserPreferenceGenre.cs
│   ├── UserPreferenceFoodRestriction.cs
│   ├── UserPreferenceActivity.cs
│   ├── UserPreferenceArtist.cs
│   ├── Group.cs
│   ├── GroupMember.cs
│   ├── GroupPreferences.cs
│   ├── GroupPreferenceGenre.cs
│   ├── GroupPreferenceFoodRestriction.cs
│   ├── GroupPreferenceActivity.cs
│   ├── GroupPreferenceArtist.cs
│   └── Plan.cs
├── Persistence/
│   ├── PlansDbContext.cs
│   ├── PlansDbContextFactory.cs
│   └── Configurations/
│       ├── UserConfiguration.cs
│       ├── RefreshTokenConfiguration.cs
│       ├── UserPreferencesConfiguration.cs
│       ├── UserPreferenceGenreConfiguration.cs
│       ├── UserPreferenceFoodRestrictionConfiguration.cs
│       ├── UserPreferenceActivityConfiguration.cs
│       ├── UserPreferenceArtistConfiguration.cs
│       ├── GroupConfiguration.cs
│       ├── GroupMemberConfiguration.cs
│       ├── GroupPreferencesConfiguration.cs
│       ├── GroupPreferenceGenreConfiguration.cs
│       ├── GroupPreferenceFoodRestrictionConfiguration.cs
│       ├── GroupPreferenceActivityConfiguration.cs
│       ├── GroupPreferenceArtistConfiguration.cs
│       └── PlanConfiguration.cs
└── Migrations/
    └── <timestamp>_InitialPlansSchema.cs   (generated)
```

Packages already in `Reshape.ElectricAi.Plans.csproj` (no new installs):
- `Microsoft.EntityFrameworkCore` 10.0.*
- `Microsoft.EntityFrameworkCore.Design` 10.0.8
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.*

---

## 3. Core types

### `Category` enum
```csharp
namespace Reshape.ElectricAi.Core.Enums;

public enum Category
{
    General,
    Transport,
    Accommodation,
    Food,
    Music,
    Lineup,
    Activity,
    Weather,
    Rules,
    Ticket,
    Safety,
    Health
}
```

### `ICategorizable` interface
```csharp
namespace Reshape.ElectricAi.Core.Domain;

using Reshape.ElectricAi.Core.Enums;

public interface ICategorizable
{
    IReadOnlyCollection<Category> Categories { get; }
}
```

No Plans entity implements `ICategorizable`. AiChat (chat messages, FAQ hot questions) and LiveFeed (feed entries) will implement it in their own libs.

### Per-dimension enums
```csharp
public enum UserRole          { User, Organizer }
public enum TicketType        { Standard, Vip, UltraVip, Black }
public enum TransportMode     { RideShare, Car, EcTrain, EcBus, Helicopter }
public enum Accommodation     { VillageRental, Camping, CarCamping, RvCamping, Glamping }
public enum MusicGenre        { HipHop, House, Balkan, Rock, Folk, Techno, Pop, Electronic, Jazz, Metal, Other }
public enum FoodRestriction   { Vegan, Vegetarian, NoPeanuts, NoMeat, NoPork, NoDairy, NoGluten, NoShellfish, NoEggs, Halal, Kosher }
public enum ActivityType      { Relax, Energetic, Adrenaline, Social, Creative, Wellness, Discovery }
public enum AgeGroup          { Under18, Adult18To24, Adult25To34, Adult35To44, Adult45Plus }
public enum PlanScope         { Individual, Group }
public enum PlanState         { NoPrefs, Partial, Ready, Generated }
```

**Evolution rule:** appending new values is safe (DB stores as string). Reordering is forbidden — would not affect string-converted columns directly, but stays a discipline so int-mapped consumers don't break later.

---

## 4. Entities

All entities use:
- `Guid` PKs generated client-side via `Guid.CreateVersion7()` (time-ordered v7 UUIDs, .NET 9+ API).
- `DateTime` columns mapped to Postgres `timestamp with time zone` (Npgsql default for UTC kind).
- `[Column(TypeName = "...")]` only where the default mapping needs override (`jsonb` on `Plan.ContentJson`).

### 4.1 `User` — table `Users`
| Column | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Email | string(256) | unique, lowercase-normalized at app layer |
| PasswordHash | string(200) | BCrypt output |
| PasswordSalt | byte[] | 16 bytes from `RandomNumberGenerator.GetBytes(16)` |
| Role | UserRole | enum string |
| CreatedUtc | DateTime | |
| UpdatedUtc | DateTime | |
| xmin | uint | concurrency token (`.UseXminAsConcurrencyToken()`) |

Index: unique on `Email`.

### 4.2 `RefreshToken` — table `RefreshTokens`
| Column | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| UserId | Guid | FK → `Users.Id`, ON DELETE CASCADE |
| TokenHash | string(88) | SHA-256 base64, unique |
| CreatedUtc | DateTime | |
| ExpiresUtc | DateTime | |
| RevokedUtc | DateTime? | null = active |
| ReplacedByHash | string(88)? | set on rotation |

Indexes:
- unique on `TokenHash`
- filtered on `(UserId)` WHERE `RevokedUtc IS NULL` — fast active-token lookup at login/single-session enforcement.

### 4.3 `UserPreferences` — table `UserPreferences`
1:1 with User. UserId is both PK and FK.

| Column | Type | Notes |
|---|---|---|
| UserId | Guid | PK + FK → `Users.Id`, ON DELETE CASCADE |
| TicketType | TicketType? | enum string |
| Accommodation | Accommodation? | enum string |
| Transport | TransportMode? | enum string |
| AgeGroup | AgeGroup? | enum string |
| UpdatedUtc | DateTime | |
| xmin | uint | concurrency token |

**Multi-value preferences are child tables** (see 4.4–4.7). Composition collections on UserPreferences:
```csharp
public List<UserPreferenceGenre> Genres { get; set; } = [];
public List<UserPreferenceFoodRestriction> FoodRestrictions { get; set; } = [];
public List<UserPreferenceActivity> Activities { get; set; } = [];
public List<UserPreferenceArtist> Artists { get; set; } = [];
```

**README divergence:** the `foodPreferences` and `allergens` fields in `README.md` collapse into a single `FoodRestrictions` set per user clarification. README + PROJECT.md docs will be updated in this task.

### 4.4 `UserPreferenceGenre` — table `UserPreferenceGenres`
| Column | Type | Notes |
|---|---|---|
| UserId | Guid | PK part, FK → `UserPreferences.UserId`, ON DELETE CASCADE |
| Genre | MusicGenre | PK part, enum string |

Composite PK `(UserId, Genre)`. Blocks duplicates by construction.

### 4.5 `UserPreferenceFoodRestriction` — table `UserPreferenceFoodRestrictions`
| Column | Type | Notes |
|---|---|---|
| UserId | Guid | PK part, FK → `UserPreferences.UserId`, ON DELETE CASCADE |
| Restriction | FoodRestriction | PK part, enum string |

### 4.6 `UserPreferenceActivity` — table `UserPreferenceActivities`
| Column | Type | Notes |
|---|---|---|
| UserId | Guid | PK part, FK → `UserPreferences.UserId`, ON DELETE CASCADE |
| Activity | ActivityType | PK part, enum string |

### 4.7 `UserPreferenceArtist` — table `UserPreferenceArtists`
| Column | Type | Notes |
|---|---|---|
| UserId | Guid | PK part, FK → `UserPreferences.UserId`, ON DELETE CASCADE |
| ArtistName | string(200) | PK part — free-form artist name (matched against lineup later) |

### 4.8 `Group` — table `Groups`
| Column | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| OwnerUserId | Guid | FK → `Users.Id`, ON DELETE RESTRICT (deleting a user with active groups requires explicit transfer) |
| Name | string(100) | |
| CreatedUtc | DateTime | |

### 4.9 `GroupMember` — table `GroupMembers`
| Column | Type | Notes |
|---|---|---|
| GroupId | Guid | PK part, FK → `Groups.Id`, ON DELETE CASCADE |
| UserId | Guid | PK part, FK → `Users.Id`, ON DELETE CASCADE |
| JoinedUtc | DateTime | |

Composite PK `(GroupId, UserId)`.

### 4.10 `GroupPreferences` — table `GroupPreferences`
Same shape as `UserPreferences` but FK on `Group`.

| Column | Type | Notes |
|---|---|---|
| GroupId | Guid | PK + FK → `Groups.Id`, ON DELETE CASCADE |
| TicketType | TicketType? | |
| Accommodation | Accommodation? | |
| Transport | TransportMode? | |
| AgeGroup | AgeGroup? | |
| UpdatedUtc | DateTime | |
| xmin | uint | concurrency token |

Collections: `GroupPreferenceGenre`, `GroupPreferenceFoodRestriction`, `GroupPreferenceActivity`, `GroupPreferenceArtist`.

### 4.11–4.14 Group preference child tables
Same structure as user variants — `GroupId` replaces `UserId`, composite PK with the value column.

### 4.15 `Plan` — table `Plans`
| Column | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Scope | PlanScope | enum string |
| OwnerUserId | Guid? | FK → `Users.Id`, ON DELETE CASCADE — set when Scope=Individual |
| GroupId | Guid? | FK → `Groups.Id`, ON DELETE CASCADE — set when Scope=Group |
| TicketType | TicketType | enum string, snapshot at generation |
| ContentJson | string | `[Column(TypeName = "jsonb")]` — full PlanDto serialized |
| GeneratedUtc | DateTime | |
| ExportedUtc | DateTime? | null until first export |
| xmin | uint | concurrency token |

CHECK constraint:
```sql
CONSTRAINT ck_plans_owner_xor_group
  CHECK ((OwnerUserId IS NULL) <> (GroupId IS NULL))
```
(true ⇔ exactly one of the two is non-null)

`PlanState` is **not** persisted — derived at read time from preferences completion + `GeneratedUtc`.

---

## 5. `PlansDbContext`

```csharp
namespace Reshape.ElectricAi.Plans.Persistence;

using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Plans.Entities;

public class PlansDbContext(DbContextOptions<PlansDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<UserPreferenceGenre> UserPreferenceGenres => Set<UserPreferenceGenre>();
    public DbSet<UserPreferenceFoodRestriction> UserPreferenceFoodRestrictions => Set<UserPreferenceFoodRestriction>();
    public DbSet<UserPreferenceActivity> UserPreferenceActivities => Set<UserPreferenceActivity>();
    public DbSet<UserPreferenceArtist> UserPreferenceArtists => Set<UserPreferenceArtist>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupPreferences> GroupPreferences => Set<GroupPreferences>();
    public DbSet<GroupPreferenceGenre> GroupPreferenceGenres => Set<GroupPreferenceGenre>();
    public DbSet<GroupPreferenceFoodRestriction> GroupPreferenceFoodRestrictions => Set<GroupPreferenceFoodRestriction>();
    public DbSet<GroupPreferenceActivity> GroupPreferenceActivities => Set<GroupPreferenceActivity>();
    public DbSet<GroupPreferenceArtist> GroupPreferenceArtists => Set<GroupPreferenceArtist>();
    public DbSet<Plan> Plans => Set<Plan>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("plans");
        builder.ApplyConfigurationsFromAssembly(typeof(PlansDbContext).Assembly);
    }
}
```

`IEntityTypeConfiguration<T>` per entity keeps `OnModelCreating` clean.

### Configuration responsibilities
Each `*Configuration` class encodes:
- `HasKey(...)` — single or composite
- `Property(x => x.SomeEnum).HasConversion<string>().HasMaxLength(40)` for every enum column
- `Property(x => x.Email).HasMaxLength(256)` etc. for length-bounded strings
- `HasIndex(...).IsUnique()` for `Users.Email`, `RefreshTokens.TokenHash`
- Filtered index for active refresh tokens
- `HasOne(...).WithMany(...).HasForeignKey(...).OnDelete(DeleteBehavior.Cascade | Restrict)`
- `ToTable("...", t => t.HasCheckConstraint("ck_plans_owner_xor_group", "..."))` on Plan
- `Property(x => x.ContentJson).HasColumnType("jsonb")` on Plan
- `.UseXminAsConcurrencyToken()` on `User`, `UserPreferences`, `GroupPreferences`, `Plan` (Npgsql-idiomatic system-column token; no extra column added)

---

## 6. Design-time factory

```csharp
namespace Reshape.ElectricAi.Plans.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class PlansDbContextFactory : IDesignTimeDbContextFactory<PlansDbContext>
{
    public PlansDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("RESHAPE_PLANS_CONNECTION")
            ?? "Host=localhost;Database=electric_ai;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<PlansDbContext>()
            .UseNpgsql(connection, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "plans"))
            .Options;

        return new PlansDbContext(options);
    }
}
```

Lets `dotnet ef` work without Presentation startup wiring. Connection string overridable via env var.

---

## 7. Migration generation

```
dotnet ef migrations add InitialPlansSchema `
  -p src/Reshape.ElectricAi.Plans `
  -s src/Reshape.ElectricAi.Plans `
  --context PlansDbContext `
  --output-dir Migrations
```

Produces `Migrations/<utc-timestamp>_InitialPlansSchema.cs` + `PlansDbContextModelSnapshot.cs`.

### Expected migration content (sanity checklist)
- `CREATE SCHEMA IF NOT EXISTS plans;`
- Tables in PascalCase: `plans."Users"`, `plans."RefreshTokens"`, `plans."UserPreferences"`, 4 user preference child tables, `plans."Groups"`, `plans."GroupMembers"`, `plans."GroupPreferences"`, 4 group preference child tables, `plans."Plans"`.
- Enum columns declared as `text` with explicit length where applicable.
- `jsonb` column type for `Plans.ContentJson`.
- CHECK constraint `ck_plans_owner_xor_group` on `plans."Plans"`.
- Unique indexes on `Users.Email` and `RefreshTokens.TokenHash`.
- Filtered index on `RefreshTokens(UserId)` WHERE `RevokedUtc IS NULL`.
- FK cascades as listed in §4.
- History table `plans.__EFMigrationsHistory`.

---

## 8. Documentation alignment (in-scope changes)

Update Plans-schema table references from snake_case to PascalCase:
- `PROJECT.md` — "Data model overview" table row for `plans`
- `README.md` — any plans-schema references in canonical schemas section
- `CODE.md` — `plans.refresh_tokens` example in Auth section → `plans."RefreshTokens"`

Add the 8 preference child tables to PROJECT.md's table list.

Cross-schema examples for other libs (vector, feed, chat) stay untouched — their owners pick conventions.

---

## 9. Out of scope (deferred to follow-up plans)

- `PlansModule` DI extension
- Any service classes / interfaces in Core
- Controllers, DTOs, validators
- `Program.cs` wiring for `AddDbContext<PlansDbContext>` and startup migration
- Seed data
- `Reshape.ElectricAi.Plans.Tests` project
- AiChat / LiveFeed `ICategorizable` consumers
- Lineup table or artist FK (artists remain free-form strings)

---

## 10. Risks + mitigations

| Risk | Mitigation |
|---|---|
| Enum value reordering breaks existing DB rows when string-converted | Discipline — append-only changes; documented in §3 |
| PascalCase identifiers require double-quoting in `psql` | Accepted trade-off (user chose); team uses GUI tools mostly |
| `xmin` concurrency token surprises devs unfamiliar with Postgres | Documented here; only on 4 mutation-heavy entities |
| Empty `UserPreferences` row not auto-created on register | Flagged for next plan (services); migration still succeeds without it |
| `Plan.ContentJson` shape changes break old plans | jsonb is schema-flexible; deserialization handles missing keys in code |
