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

## Running locally
- `docker compose -f docker/docker-compose.yml up -d` starts Postgres + OpenFGA
- `dotnet run --project src/CardTrader.Web` starts the Blazor app
- `dotnet test tests/CardTrader.Authorization.Tests` runs the adversarial suite