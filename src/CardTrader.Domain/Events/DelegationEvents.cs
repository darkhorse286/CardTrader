using CardTrader.Domain.Common;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Events;

public sealed record DelegationCreated(
    DelegationId Id, UserId DelegatorId, UserId DelegateeId) : IDomainEvent;

// ExpiresAt is null for indefinite delegations, non-null for time-bound ones.
public sealed record DelegationActivated(
    DelegationId Id, UserId DelegatorId, DateTimeOffset? ExpiresAt) : IDomainEvent;

public sealed record DelegationRevoked(
    DelegationId Id, UserId DelegatorId) : IDomainEvent;
