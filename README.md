# CardTrader

A proof-of-concept .NET application demonstrating relationship-based access control (ReBAC) using [OpenFGA](https://openfga.dev). Every authorization decision is made by a dedicated authorization layer. No permission logic appears in application or UI code.

The domain is a trading card exchange. Users own card instances, organize them into rosters, and propose trades with each other. A delegation feature lets one user supervise another's account. Every object-level permission is decided by evaluating a relationship graph in FGA: who can view this trade, who can cancel it, whether a delegated parent can act on a child's behalf.

## Architecture

Authorization logic lives in `authz/model.fga` and is evaluated by an OpenFGA server at runtime. Application services call `IAuthorizationService.CheckAsync` and act on the result. A domain event pipeline keeps the tuple graph consistent without authorization logic leaking into domain or application code.

See [docs/architecture.md](docs/architecture.md) for the full design: authorization model, domain model, layer boundaries, event pipeline, check flow, and test strategy.

## Getting started

**Prerequisites**

- Docker Desktop
- .NET 10 SDK
- `fga` CLI binary at `tools/fga.exe` (Windows) or on `$PATH` (macOS/Linux). Download from https://github.com/openfga/cli/releases, or install via `brew install openfga/tap/fga` on macOS.

**Windows (automated)**

```powershell
.\Start-Dev.ps1
```

This starts Postgres and OpenFGA in Docker, applies EF migrations, loads the FGA model, wires the store IDs into user-secrets, and launches the app at `http://localhost:5057`.

**Manual**

```powershell
# Start Postgres and OpenFGA
docker compose -f docker/docker-compose.yml up -d

# Load the authorization model (prints the user-secrets commands to run next)
.\authz\load-model.ps1          # Windows
bash authz/load-model.sh        # macOS/Linux

# Apply EF migrations
# Direct dotnet ef database update does not work from a Windows host against Docker Postgres.
# Use the generate-then-execute pattern documented in CLAUDE.md.

# Run the app
dotnet run --project src/CardTrader.Web
```

## Running tests

The Testcontainers-backed test projects require Docker Desktop to be running.

```powershell
dotnet test CardTrader.slnx
```

To run a specific layer:

```powershell
dotnet test tests/CardTrader.Authorization.Tests   # adversarial FGA evidence suite
dotnet test tests/CardTrader.Integration.Tests     # full stack with real FGA and Postgres
dotnet test tests/CardTrader.Application.Tests     # service behavior with mocked authorization
```

## Demo walkthrough

[DEMO.md](DEMO.md) walks through the five key authorization scenarios in the browser UI.

## Project structure

```
authz/                          FGA model (model.fga) and provisioning scripts
docker/                         Docker Compose for Postgres and OpenFGA
docs/                           Architecture, ADRs, known limitations
src/
  CardTrader.Domain             Entities, domain events, repository interfaces
  CardTrader.Application        Application services, IAuthorizationService interface
  CardTrader.Authorization      FGA type and relation name constants
  CardTrader.Infrastructure     OpenFGA client, EF Core, TupleWriters, repositories
  CardTrader.Identity           ASP.NET Core Identity, admin and current-user services
  CardTrader.Web                Blazor Server app, DI composition, demo seeding
tests/
  CardTrader.Domain.Tests
  CardTrader.Application.Tests
  CardTrader.Authorization.Tests    Adversarial suite against a real FGA engine
  CardTrader.Identity.Tests
  CardTrader.Infrastructure.Tests
  CardTrader.Integration.Tests      Full stack with real FGA and Postgres
```

## Known limitations

This PoC has several design decisions that have production implications, including permanent FGA tuples on settled trades and snapshot-based supervisor membership. See [docs/limitations.md](docs/limitations.md).

## Design decisions

Significant architectural decisions are recorded as ADRs in [docs/adr/](docs/adr/).
