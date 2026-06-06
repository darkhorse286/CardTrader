# CardTrader Architecture

## What this PoC demonstrates

CardTrader is a proof-of-concept .NET application that shows how relationship-based access control (ReBAC) can be implemented using [OpenFGA](https://openfga.dev), an open-source implementation of Google's Zanzibar authorization system.

The central claim is that access decisions for a trading card application, where access depends on object ownership, roster membership, trade participation, and delegated supervision, can be expressed in a declarative relationship model. Application services ask the authorization layer for a decision and act on the answer. No permission logic appears in service or UI code.

The key architectural constraint: `IAuthorizationService` is the only authorization interface the application layer touches. The OpenFGA SDK is never imported outside `CardTrader.Infrastructure`.

## Authorization model

The model lives in `authz/model.fga`. It defines four object types. Each type has direct relations (tuples written at runtime) and computed permissions (derived from those relations using set operations).

### Types and relations

**user** is the identity leaf type. It has no relations.

**card_instance** is the owned object. FGA tuples attach to instances, not to the card archetype.

| Relation | Definition |
|---|---|
| `owner` | Direct grant to a `user` |
| `viewer` | Direct grant to a `user` (modeled; write path not yet implemented) |
| `roster` | Membership link to a `roster` object |
| `can_view` | `owner OR viewer OR can_view from roster` |
| `can_manage` | `owner` |
| `can_trade` | `owner` |

Visibility flows through the roster graph: if a user can view a roster, they can view all instances in it.

**roster** groups card instances. The `viewer` relation accepts the wildcard `user:*`, which makes a roster public.

| Relation | Definition |
|---|---|
| `owner` | Direct grant to a `user` |
| `viewer` | Direct grant to a `user` or `user:*` (public) |
| `can_view` | `owner OR viewer` |
| `can_manage` | `owner` |

**trade_proposal** carries per-proposal per-role access. The `supervisor` relation is a userset reference: it resolves to whichever users hold the `active_delegator` relation on a linked delegation object, connecting the delegation graph to the trade graph without writing user IDs directly.

| Relation | Definition |
|---|---|
| `initiator` | Direct grant to a `user` |
| `recipient` | Direct grant to a `user` |
| `supervisor` | Userset: `delegation#active_delegator` |
| `facilitator` | Direct grant to a `user` |
| `can_view` | `initiator OR recipient OR supervisor OR facilitator` |
| `can_accept` | `recipient` |
| `can_cancel` | `initiator OR supervisor` |

**delegation** is a first-class entity representing a supervision relationship between two users.

| Relation | Definition |
|---|---|
| `delegator` | Direct grant to a `user` |
| `delegatee` | Direct grant to a `user` |
| `active_delegator` | Union of indefinite `[user]` and time-bound `[user with not_expired]` |

The `active_delegator` union is important: using only `[user with not_expired]` would prevent writing unconditional tuples for indefinite delegations. Both forms are needed.

### Condition

```
condition not_expired(current_time: timestamp, expires_at: timestamp) {
  current_time < expires_at
}
```

`not_expired` is used on `active_delegator` tuples for time-bound delegations. The `expires_at` timestamp is baked into the tuple at write time. The `current_time` is supplied at evaluation time. Both `CheckAsync` and `ListObjectsAsync` in `OpenFgaAuthorizationService` pass `current_time = DateTimeOffset.UtcNow` so conditions are evaluated consistently across both query types.

## Domain model

| Entity | Role |
|---|---|
| `Card` | Archetype, admin-created. No FGA tuples. |
| `CardInstance` | Owned object. An `owner` tuple is written when the instance is created. |
| `Roster` | Groups instances. An `owner` tuple is written on creation. |
| `TradeProposal` | Per-proposal access. `initiator`, `recipient`, and zero or more `supervisor` tuples are written on creation. |
| `Delegation` | Supervision relationship. `delegator` and `delegatee` tuples are written on creation; `active_delegator` is written when the delegation is activated and deleted when it is revoked. |

Value objects (`UserId`, `CardInstanceId`, `RosterId`, and so on) wrap `Guid` and are type-incompatible with each other, preventing cross-type identifier confusion.

## Architecture layers

The solution follows a strict inward dependency rule: each layer depends only on layers below it. The OpenFGA SDK is visible only to Infrastructure. The authorization model constants are shared upward through a thin constants project.

```
CardTrader.Web              Blazor Server, DI composition, demo seeding
CardTrader.Application      Application services, IAuthorizationService interface
CardTrader.Authorization    FGA type and relation name constants
CardTrader.Infrastructure   OpenFGA client, EF Core, TupleWriters, repositories
CardTrader.Identity         ASP.NET Core Identity, IAdminService, ICurrentUser
CardTrader.Domain           Entities, domain events, repository interfaces
```

`CardTrader.Application` depends on `Domain` and `Authorization`. It never references the OpenFGA SDK. `CardTrader.Infrastructure` implements the interfaces defined in `Application` and `Domain`.

Admin actions (adding cards, viewing all pending trades, cancelling any trade) are authorized via an ASP.NET Core role claim checked through `IAdminService`, not through FGA. This is intentional: the admin bypass is additive and requires no FGA tuple management. The tradeoff is that admin actions are not auditable through the authorization model. See [docs/limitations.md](limitations.md).

## Event-driven tuple pipeline

Domain mutations raise domain events. FGA tuple writes are handled by `TupleWriterDispatcher`, which routes each event to the appropriate handler. No tuple writes happen inline in application services.

```
Application service
  -> Domain entity raises event (e.g. CardInstanceCreated)
  -> IDomainEventDispatcher.DispatchAsync
  -> TupleWriterDispatcher
  -> ITupleWriterHandler (e.g. CardInstanceTupleWriter)
  -> IFgaWriteClient
  -> OpenFGA
```

Each entity type has a dedicated writer:

| Writer | Events handled |
|---|---|
| `CardInstanceTupleWriter` | `CardInstanceCreated`, `CardInstanceAddedToRoster`, `CardInstanceRemovedFromRoster`, `CardInstanceOwnershipTransferred` |
| `RosterTupleWriter` | `RosterCreated`, `RosterSharedWithUser`, `RosterUnshared`, `RosterMadePublic`, `RosterMadePrivate` |
| `DelegationTupleWriter` | `DelegationCreated`, `DelegationActivated`, `DelegationRevoked` |
| `TradeProposalTupleWriter` | `TradeProposalCreated`, `TradeProposalFacilitatorAssigned` |

`TradeProposalTupleWriter` queries active delegations at proposal creation time and writes supervisor tuples for any delegation where a participant is the delegatee. Events for accepted and cancelled proposals are intentionally not handled: the tuples remain as audit state. See [docs/limitations.md](limitations.md) for the production implications.

## Authorization check flow

Each application service calls `IAuthorizationService.CheckAsync(user, relation, object)` before performing a mutation. The implementation sends a request to the OpenFGA HTTP API and returns a `bool`. If the check returns false, the service throws `UnauthorizedAccessException`.

```
RosterService.ShareWithUserAsync
  -> IAuthorizationService.CheckAsync("user:{id}", "can_manage", "roster:{id}")
  -> OpenFgaAuthorizationService (Infrastructure)
  -> OpenFGA HTTP /check with current_time context
  <- allowed: true or false
  -> UnauthorizedAccessException if false
```

`ListObjectsAsync` is used by `RosterService.GetAllVisibleAsync` to find all rosters a user can view. It passes the same `current_time` context so time-bound delegation conditions are evaluated correctly.

## Test strategy

Six test projects cover the solution. Each layer is tested independently, and no layer's tests substitute for another's.

| Project | Approach | Infrastructure |
|---|---|---|
| `CardTrader.Domain.Tests` | Unit tests for entities, events, and value objects | None |
| `CardTrader.Application.Tests` | Unit tests for service behavior given an allow or deny decision | Mocked `IAuthorizationService` |
| `CardTrader.Identity.Tests` | Unit tests for Identity adapters | Mocked Identity |
| `CardTrader.Infrastructure.Tests` | Unit tests for repositories and tuple writers | SQLite / mocked FGA client |
| `CardTrader.Authorization.Tests` | Adversarial tests against a real FGA engine | Testcontainers (OpenFGA, in-memory store) |
| `CardTrader.Integration.Tests` | Full-stack tests through the service layer | Testcontainers (OpenFGA + Postgres) |

The adversarial suite in `CardTrader.Authorization.Tests/Adversarial/` is the evidence battery for the authorization model. Each test class creates its own isolated FGA store to prevent tuple state from bleeding between test classes. The FGA engine is never mocked in this project: doing so would invalidate the evidence.

## Design decisions and known limitations

Significant architectural decisions are recorded as ADRs:

- [ADR 001: OpenFGA as the authorization engine](adr/001-openfga-as-authorization-engine.md) covers why OpenFGA was chosen over OPA, Casbin, Oso, and a custom implementation.
- [ADR 002: Event-driven tuple writes](adr/002-event-driven-tuple-writes.md) covers why tuple writes are handled by domain event listeners rather than inline in application services.

Production limitations of this PoC are documented in [docs/limitations.md](limitations.md). Read this before using the codebase as a template.

## Running locally

**Prerequisites:** Docker Desktop, .NET 10 SDK, `fga` CLI binary at `tools/fga.exe` (Windows) or on `$PATH` (macOS/Linux). Download the CLI from https://github.com/openfga/cli/releases.

**Windows (one script):**

```powershell
.\Start-Dev.ps1
```

This starts Postgres and OpenFGA in Docker, applies EF migrations inside the container, loads the authorization model, wires the store and model IDs into user-secrets, and launches the Blazor app at `http://localhost:5057`.

**Manual steps:**

```powershell
# Start infrastructure
docker compose -f docker/docker-compose.yml up -d

# Load the FGA model; follow the printed user-secrets commands
.\authz\load-model.ps1          # Windows
bash authz/load-model.sh        # macOS/Linux

# Apply EF migrations via generate-then-execute
# (Direct dotnet ef database update fails from a Windows host against Docker Postgres.)
# See CLAUDE.md for the full rationale and commands.

# Run the app
dotnet run --project src/CardTrader.Web
```

The app opens at `http://localhost:5057`. The demo seeder creates an admin account and seed data on first run.
