# ADR 002: Event-driven tuple writes

## Status

Accepted

## Context

When a domain mutation occurs, for example when a card instance is created or a roster is shared with a user, the corresponding FGA relationship tuples must be written. Two implementation patterns were available.

**Inline writes** would have application services call `IFgaWriteClient` directly, immediately after persisting the domain mutation. The service knows what changed and can write the corresponding tuples in the same method.

The problem with inline writes is that authorization concerns leak into the application layer. `CardInstanceService.CreateAsync` would need to know that creating a card instance requires writing an `owner` tuple. `RosterService.ShareWithUserAsync` would need to know the FGA relation name and object format for a viewer grant. The Application layer would import FGA constants and become aware of the tuple structure, violating the constraint that application services should not know how authorization is enforced.

Inline writes also scatter tuple write logic across multiple service methods. Adding a new tuple write behavior, for example writing a new relation when a card instance is created, would require finding and modifying every relevant service method.

**Event-driven writes** use the domain event pattern already present in the domain model. Each domain entity raises events for every significant state change. A dispatcher routes each event to a dedicated tuple writer. The tuple writer calls `IFgaWriteClient` and owns the mapping from domain event to FGA tuple format.

This separates the what (the domain mutation, expressed as a domain event) from the how (the FGA tuple write, expressed in the tuple writer). Application services raise events. They do not know how those events are handled.

## Decision

Use event-driven tuple writes via `TupleWriterDispatcher` and `ITupleWriterHandler` implementations.

Domain entities raise events for every mutation. `IDomainEventDispatcher` is called by application services after persisting the mutated entity. `TupleWriterDispatcher` implements `IDomainEventDispatcher` and routes each event to the first handler that claims it via `CanHandle`. Each entity type has a dedicated handler in `CardTrader.Infrastructure/TupleWriters/`.

Application services have no knowledge of FGA tuple formats or relation names beyond the constants in `CardTrader.Authorization`. The OpenFGA SDK is never called from application services.

## Consequences

The Application layer has zero FGA knowledge. `CardTrader.Application` does not reference the OpenFGA SDK or any tuple-level concept. Authorization enforcement is entirely an Infrastructure concern.

Tuple write logic is co-located with the events that trigger it. `CardInstanceTupleWriter` is the single place that knows which tuples to write when a card instance is created, added to a roster, removed from a roster, or transferred to a new owner. Adding a new behavior means adding a new handler or extending an existing one, without modifying service code.

Tuple writers can be tested independently. `CardTrader.Infrastructure.Tests` tests each writer with mocked `IFgaWriteClient` inputs and verifies the correct write or delete calls are made.

The tradeoffs are as follows. Tuple writes happen after the database write, not atomically with it. If the FGA write fails after the database write succeeds, the tuple graph and the database are inconsistent. This PoC does not implement a compensation pattern or an outbox to guarantee eventual consistency. In a production system with strict consistency requirements, the event dispatch and tuple write would need to participate in a transactional outbox or a saga.

`TradeProposalTupleWriter` requires access to `IDelegationRepository` to query active delegations at proposal creation time. Because the writer is registered as a singleton and `IDelegationRepository` is scoped, the writer must create a new service scope for each event it handles via `IServiceScopeFactory`. This is correct but atypical. Future writers with scoped dependencies will need the same pattern, which is worth documenting as a convention if the codebase grows.

Tuple cleanup for settled trade proposals was deliberately omitted. `TradeProposalAccepted` and `TradeProposalCancelled` events are raised but no writer handles them. The tuples remain as audit state. See [docs/limitations.md](../limitations.md) for the production implications of this choice.
