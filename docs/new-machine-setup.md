# New Machine Setup

Requirements and known gotchas for getting this project running on a fresh machine.

---

## Prerequisites

| Tool | Version | Check |
|---|---|---|
| .NET SDK | 10.0+ | `dotnet --version` → `10.x` |
| PostgreSQL | 18 | `psql --version` |
| **pgvector extension** | 0.7.0+ | see below |
| OpenAI account | — | API key with credits |
| Git | any recent | — |

---

## PostgreSQL + pgvector

The `VectorDb` migration runs `CREATE EXTENSION vector` automatically via EF Core (`HasPostgresExtension("vector")` in `VectorDbContext`). If the `pgvector` shared library is not installed on the OS, the migration fails with:

```
Npgsql.PostgresException (0x80004005): extension "vector" is not available
DETAIL:  Could not open extension control file "/usr/share/postgresql/16/extension/vector.control": No such file or directory.
```

### Fix: install pgvector

**Ubuntu / Debian:**
```bash
sudo apt install postgresql-18-pgvector
```

**macOS (Homebrew):**
```bash
brew install pgvector
```

If Postgres was installed via Homebrew, you may need to restart it:
```bash
brew services restart postgresql@16
```

**Windows:**
Download the pre-built binaries from [pgvector releases](https://github.com/pgvector/pgvector/releases) and follow the install instructions for your Postgres version.

### Verify pgvector is available

Connect to your database and run:
```sql
SELECT * FROM pg_available_extensions WHERE name = 'vector';
```

You should see a row. If the result is empty, the OS package is not installed.

The extension itself does **not** need to be created manually — the EF Core migration creates it automatically in the `electric_ai` database when you run `dotnet ef database update`.

---

## Database setup

1. Create the database:
   ```sql
   CREATE DATABASE electric_ai;
   ```

2. Run all migrations (one per `DbContext`):
   ```bash
   dotnet ef database update -p src/Reshape.ElectricAi.Plans -s src/Reshape.ElectricAi.Presentation -- --context PlansDbContext
   dotnet ef database update -p src/Reshape.ElectricAi.VectorDb -s src/Reshape.ElectricAi.Presentation -- --context VectorDbContext
   dotnet ef database update -p src/Reshape.ElectricAi.LiveFeed -s src/Reshape.ElectricAi.Presentation -- --context LiveFeedDbContext
   dotnet ef database update -p src/Reshape.ElectricAi.AiChat -s src/Reshape.ElectricAi.Presentation -- --context AiChatDbContext
   ```

   The `VectorDbContext` migration creates the `vector` schema and runs `CREATE EXTENSION vector`. It will fail if pgvector is not installed (see above).

---

## Secrets

Set via `dotnet user-secrets` in the `Presentation` project:

```bash
# Required
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Database=electric_ai;Username=postgres;Password=postgres" --project src/Reshape.ElectricAi.Presentation
dotnet user-secrets set "Auth:JwtSigningKey" "$(openssl rand -base64 48)" --project src/Reshape.ElectricAi.Presentation
dotnet user-secrets set "OpenAi:ApiKey" "sk-..." --project src/Reshape.ElectricAi.Presentation
```

Never commit these. See `PROJECT.md` → Configuration matrix for all available keys.

---

## Run

```bash
dotnet build
dotnet run --project src/Reshape.ElectricAi.Presentation
```

Scalar UI: `http://localhost:5217/scalar/v1`
