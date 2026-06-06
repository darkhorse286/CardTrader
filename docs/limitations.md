# Known Limitations

This document describes design decisions in the CardTrader PoC that have meaningful implications for any production use. Read this before treating the codebase as a template.

---

## ListObjectsAsync and time-bound delegation context

**This gap has been corrected in the current codebase.**

`OpenFgaAuthorizationService.CheckAsync` passes `current_time = DateTimeOffset.UtcNow` as evaluation context, which causes the `not_expired` condition on time-bound delegation tuples to be evaluated correctly. The original implementation of `ListObjectsAsync` did not pass this context.

Without `current_time`, OpenFGA's `ListObjects` query cannot evaluate `not_expired` conditions, meaning it may return objects whose access derived from an expired delegation. `CheckAsync` on those same objects would correctly return deny, creating a split-brain situation: the list shows access that the check then refuses.

The fix is in `OpenFgaAuthorizationService.ListObjectsAsync`. The adversarial tests `ListObjects_BeforeExpiry_IncludesProposal` and `ListObjects_AfterExpiry_ExcludesProposal` in `DelegationExpiryTests` prove the model-level behavior is correct when context is supplied.

**Production note:** Any future `ListObjects` call on a type whose `can_view` path flows through a conditioned relation must include the same context. This applies to both direct calls and any new service methods added to `IAuthorizationService`. The pattern in `OpenFgaAuthorizationService` is the reference implementation.

---

## Trade proposal tuples are not cleaned up on settlement

When a trade proposal is accepted or cancelled, its FGA tuples remain in place. The `initiator`, `recipient`, `supervisor`, and `facilitator` relations on the settled proposal are never deleted.

This is a deliberate design choice for this PoC: settled proposals retain their access tuples as audit state, allowing the authorization model to answer "who was party to this trade" after the fact without a separate audit log.

**Production note:** In a system with significant trade volume, this means the FGA store grows without bound. A production deployment would need either a scheduled cleanup job that deletes tuples for settled proposals past a retention window, or an archival strategy that moves old tuples to cold storage. The cleanup events (`TradeProposalAccepted`, `TradeProposalCancelled`) are already raised as domain events; adding handlers in `TradeProposalTupleWriter` is the natural extension point.

---

## Supervisor membership is fixed at proposal creation

When a trade proposal is created, `TradeProposalTupleWriter` queries the database for active delegations where either the initiator or the recipient is the delegatee. It writes a `supervisor` tuple for each such delegation. This query runs once, at creation time.

A delegation activated after the proposal is already created will not produce a supervisor tuple for that proposal. The delegator gets no `can_view` or `can_cancel` access to it.

This is a snapshot-consistency choice. Supervisor membership reflects who had active delegated authority at the moment the trade was proposed.

**Production note:** If real-time delegation propagation is required, the design would need to change. One approach: remove explicit supervisor tuples and instead define `supervisor` as a userset computed from the delegation graph at check time, without any per-proposal tuple writes. This would require model changes and benchmarking to confirm acceptable check latency.

The integration test `DelegationActivated_AfterTradeProposalCreated_DoesNotGrantSupervisorAccess` documents this behavior as intentional.

---

## Admin actions are not modeled in FGA

Admin operations (`CardService.AddAsync`, `TradeProposalService.GetAllPendingAsync`, and the admin cancel path in `TradeProposalService.CancelAsync`) are authorized via an ASP.NET Core role claim checked through `IAdminService`, not through FGA. No FGA tuples are written for admin identity.

This is intentional. The admin bypass is additive: granting a user the admin role immediately gives them admin access to all objects without requiring tuple writes for every object in the system. Removing the role immediately revokes that access. This is the correct model for a site-wide administrative role that operates across all objects.

The tradeoff is that admin actions are not visible to the FGA authorization model. You cannot ask FGA "who can cancel any trade?" and get the admin listed. Admin decisions are not captured as FGA authorization events.

**Production note:** If admin actions need to be auditable at the authorization layer, one approach is to model an `admin` type in FGA with a single system-wide object, and write a check against it before each admin operation. A simpler approach for most systems is application-level audit logging of admin actions, which provides a durable record without complicating the FGA model.

---

## card_instance.viewer is modeled but not implemented

The FGA model defines a `viewer` relation on `card_instance` for direct per-instance sharing between users. No domain event, `TupleWriter`, service method, or UI surface exists for writing tuples to this relation.

Instance visibility currently flows entirely through roster membership: a user who can view a roster can view all instances in it.

**Production note:** The relation is intentionally left in the model as a reserved extension point. If direct instance sharing is added, the required additions are: a domain event (e.g. `CardInstanceSharedWithUser`), a handler in `CardInstanceTupleWriter`, and a service method in `CardInstanceService`. The FGA model requires no changes.

Do not assume this path works by reading the model. It does not, in the current codebase.
