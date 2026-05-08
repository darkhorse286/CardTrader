using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Entities;

public sealed class Delegation : Entity<DelegationId>
{
    public UserId DelegatorId { get; private init; }
    public UserId DelegateeId { get; private init; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private Delegation() { }

    public static Delegation Create(DelegationId id, UserId delegatorId, UserId delegateeId)
    {
        if (delegatorId == delegateeId)
            throw new ArgumentException("Delegator and delegatee must be different users.");

        var delegation = new Delegation
        {
            Id = id,
            DelegatorId = delegatorId,
            DelegateeId = delegateeId,
            IsActive = false,
        };
        delegation.Raise(new DelegationCreated(id, delegatorId, delegateeId));
        return delegation;
    }

    // Activates indefinitely — writes an unconditioned active_delegator tuple.
    public void Activate()
    {
        IsActive = true;
        ExpiresAt = null;
        Raise(new DelegationActivated(Id, DelegatorId, ExpiresAt: null));
    }

    // Activates with a time-bound expiry — writes active_delegator with not_expired condition.
    public void ActivateWithExpiry(DateTimeOffset expiresAt)
    {
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Expiry must be in the future.", nameof(expiresAt));

        IsActive = true;
        ExpiresAt = expiresAt;
        Raise(new DelegationActivated(Id, DelegatorId, expiresAt));
    }

    public void Revoke()
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot revoke an inactive delegation.");

        IsActive = false;
        ExpiresAt = null;
        Raise(new DelegationRevoked(Id, DelegatorId));
    }
}
