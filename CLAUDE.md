# CardTrader

A .NET proof-of-concept demonstrating relationship-based access control 
using OpenFGA (Zanzibar) in a Blazor Server trading card application.

## Architecture rules
- All authorization logic lives in `CardTrader.Authorization`. Zero permission
  if-statements in service or UI code.
- Every domain mutation raises a domain event (see `Domain/Events/`). Tuple 
  writes are handled by TupleWriters reacting to those events — never inline.
- `IAuthorizationService` is the only interface Application layer touches.
  It never references the OpenFGA SDK directly.

## Solution structure
See `/docs/architecture.md` for the full folder hierarchy.
- `authz/` — OpenFGA DSL model and seed tuples. Lives at solution root.
- `src/` — All .NET projects.
- `tests/` — Authorization.Tests (including Adversarial/), Application.Tests, 
  Integration.Tests.
- `docker/` — docker-compose for Postgres + OpenFGA.

## Key conventions
- FGA type names and relation names are string constants in 
  `CardTrader.Authorization/Types/` and `Relations/`. Never hard-coded elsewhere.
- The `Card` entity is an archetype. `CardInstance` is the owned object. 
  Ownership tuples attach to instances, not archetypes.
- `Delegation` is a first-class entity for parent-managed accounts.

## Solution conventions
- Every new project (src or tests) must be added to `CardTrader.slnx` before the
  task is considered done. `dotnet test` at the root discovers projects solely from
  the solution file — a project not listed there is invisible to the test runner.

## Testing conventions
- `CardTrader.Authorization.Tests` runs against a real OpenFGA instance via 
  Testcontainers. Never mock the FGA engine in this project — doing so 
  invalidates the adversarial evidence.
- `CardTrader.Application.Tests` mocks `IAuthorizationService`. Tests verify 
  service behavior given an allow or deny decision, not the decision itself.
- `CardTrader.Integration.Tests` uses real infrastructure throughout.
- Every public interface gets at least one test at its own layer before 
  the layer above it is built.
- The `Adversarial/` folder in Authorization.Tests is the QED evidence battery. 
  All scenarios in the evidence section of the PoC must have a passing test here.

## Security conventions
- No secrets (passwords, connection strings with credentials, API keys, store IDs) 
  in any committed file. Use `dotnet user-secrets` for the Web project at runtime 
  and the `CARDTRADER_DB_CONNECTION` environment variable for EF migrations.
- `appsettings.Development.json` may contain non-secret dev config (URLs, log levels).
  It must never contain usernames, passwords, or token values.

## Completion conventions
- After every feature lands (build green, all tests passing), output an updated
  prioritized list of remaining features without being asked. One sentence per
  item — what it is and why it comes next in that order.

## Running locally
- `docker compose -f docker/docker-compose.yml up -d` starts Postgres + OpenFGA
- `bash authz/load-model.sh` creates the FGA store and writes the model; follow
  its printed `dotnet user-secrets` commands to wire up the Web project
- The `fga` CLI binary lives in `tools/` (gitignored). Download from
  https://github.com/openfga/cli/releases and place at `tools/fga.exe` (Windows)
  or install via `brew install openfga/tap/fga` (macOS/Linux)
- `dotnet run --project src/CardTrader.Web` starts the Blazor app
- `dotnet test tests/CardTrader.Authorization.Tests` runs the adversarial suite

## Applying EF migrations
On Windows with Docker Desktop, connections from the host to the Postgres
container traverse the Docker bridge network and are therefore subject to
SCRAM-SHA-256 password authentication (`pg_hba.conf` last rule). `dotnet ef
database update` will fail with "password authentication failed" even with the
correct credentials. Always apply migrations via the generate-then-execute
pattern instead:

```powershell
# 1. Generate idempotent SQL (no DB connection required)
$env:CARDTRADER_DB_CONNECTION = "dummy"
dotnet ef migrations script --project src/<Project> --context <Context> `
    --idempotent --output c:\Temp\migration.sql

# 2. Apply inside the container (uses trust auth, no password)
docker cp c:\Temp\migration.sql docker-postgres-1:/tmp/migration.sql
docker exec docker-postgres-1 psql -U cardtrader -d cardtrader -f /tmp/migration.sql
```

Never attempt `dotnet ef database update` directly against the Docker Postgres
instance from a Windows host — it will always fail on this machine.