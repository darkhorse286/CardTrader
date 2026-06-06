# ADR 001: OpenFGA as the authorization engine

## Status

Accepted

## Context

The CardTrader domain requires per-object, per-relationship authorization decisions that cannot be expressed cleanly with role-based access control.

"Can this user trade this card instance?" depends on who owns it, not on any global role. "Can this parent cancel this child's trade?" depends on whether an active delegation relationship exists between those two users. "Can this user view this roster?" depends on whether they are the owner, have been individually shared with, or whether the roster is public. These decisions require traversing a relationship graph at check time, not matching a user's roles against a permission list.

Four alternatives were considered before choosing OpenFGA.

**OPA (Open Policy Agent)** evaluates policies written in Rego against input data. It is strong for attribute-based decisions and is widely adopted. It does not natively model or traverse relationship graphs. Using OPA for this domain would require fetching the relevant relationship data (ownership, roster membership, delegation state) and passing it as input to each policy evaluation, moving relationship resolution into application code and creating a coupling between the policy and the application's data model.

**Casbin** runs in-process and handles RBAC and simple ABAC well. Relationship graph traversal across arbitrary object types is not a first-class feature. Expressing the delegation-to-trade-proposal supervisory path shown in this PoC would require custom model extensions.

**Oso** is relationship-aware and supports a policy language that can express ownership and role relationships. At the time this PoC was written, Oso's community and production track record were smaller than OpenFGA's, and its hosted offering had been discontinued in favour of an open-source library model that was still maturing.

**Custom implementation** would require writing relationship graph traversal from scratch, including indexed tuple storage, userset evaluation, and a query engine. This is the approach Google described in the Zanzibar paper. The engineering cost is high and the prior art is limited to large organizations with dedicated infrastructure teams.

OpenFGA is an open-source implementation of the Zanzibar specification, maintained as a CNCF project. It provides a declarative DSL for expressing authorization models, first-class support for userset traversal (the delegation-to-supervisor path relies on this), conditional tuples with typed parameters, a `ListObjects` query for enumerating accessible objects, and official Docker images that work with Testcontainers for test isolation.

## Decision

Use OpenFGA as the authorization engine.

The authorization model lives in `authz/model.fga`. Application code never evaluates authorization logic directly. All authorization decisions flow through `IAuthorizationService`, which is implemented by `OpenFgaAuthorizationService` in `CardTrader.Infrastructure`. The OpenFGA SDK is never imported outside the Infrastructure project.

## Consequences

Using OpenFGA produces the following outcomes.

The authorization model is expressed in a single, version-controlled DSL file. Changes to the authorization model are visible as diffs to that file. No authorization logic is scattered across application code.

The FGA model is deny-by-default. A user with no relation to an object gets no access. There is no code path where a missing check results in accidental access.

The `ListObjects` and `Expand` query surfaces are available for deriving object sets from the relationship graph without application-level filtering loops.

The adversarial test suite runs against a real FGA instance via Testcontainers. The evidence is not synthetic: it uses the same engine that runs in production.

The tradeoffs are as follows. OpenFGA is an external process that must be running for the application to function. Every authorization check is an HTTP round-trip to that process. There is no fallback if FGA is unavailable; the application fails open or closed depending on how the caller handles the exception. The OpenFGA SDK couples `CardTrader.Infrastructure` to a specific client library, meaning SDK version upgrades are Infrastructure changes. Writing a test that mocks the FGA engine is possible but would invalidate the adversarial evidence, which is why the adversarial suite never does so.
