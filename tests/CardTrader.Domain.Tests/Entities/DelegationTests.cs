using CardTrader.Domain.Entities;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Tests.Entities;

public sealed class DelegationTests
{
    private static readonly DelegationId Id = DelegationId.New();
    private static readonly UserId Parent = UserId.New();
    private static readonly UserId Child = UserId.New();

    private static readonly DateTimeOffset FutureExpiry = DateTimeOffset.UtcNow.AddYears(1);
    private static readonly DateTimeOffset PastExpiry   = DateTimeOffset.UtcNow.AddDays(-1);

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsProperties()
    {
        var deleg = Delegation.Create(Id, Parent, Child);

        Assert.Equal(Id, deleg.Id);
        Assert.Equal(Parent, deleg.DelegatorId);
        Assert.Equal(Child, deleg.DelegateeId);
        Assert.False(deleg.IsActive);
        Assert.Null(deleg.ExpiresAt);
    }

    [Fact]
    public void Create_RaisesDelegationCreated()
    {
        var deleg = Delegation.Create(Id, Parent, Child);

        var evt = Assert.Single(deleg.DomainEvents);
        var created = Assert.IsType<DelegationCreated>(evt);
        Assert.Equal(Id, created.Id);
        Assert.Equal(Parent, created.DelegatorId);
        Assert.Equal(Child, created.DelegateeId);
    }

    [Fact]
    public void Create_SameDelegatorAndDelegatee_Throws()
        => Assert.Throws<ArgumentException>(() => Delegation.Create(Id, Parent, Parent));

    // ── Activate (indefinite) ────────────────────────────────────────────────

    [Fact]
    public void Activate_SetsIsActiveAndRaisesEvent()
    {
        var deleg = Delegation.Create(Id, Parent, Child);
        deleg.ClearDomainEvents();

        deleg.Activate();

        Assert.True(deleg.IsActive);
        Assert.Null(deleg.ExpiresAt);
        var evt = Assert.Single(deleg.DomainEvents);
        var activated = Assert.IsType<DelegationActivated>(evt);
        Assert.Equal(Id, activated.Id);
        Assert.Equal(Parent, activated.DelegatorId);
        Assert.Null(activated.ExpiresAt);
    }

    // ── ActivateWithExpiry (time-bound) ──────────────────────────────────────

    [Fact]
    public void ActivateWithExpiry_FutureDate_SetsExpiryAndRaisesEvent()
    {
        var deleg = Delegation.Create(Id, Parent, Child);
        deleg.ClearDomainEvents();

        deleg.ActivateWithExpiry(FutureExpiry);

        Assert.True(deleg.IsActive);
        Assert.Equal(FutureExpiry, deleg.ExpiresAt);
        var evt = Assert.Single(deleg.DomainEvents);
        var activated = Assert.IsType<DelegationActivated>(evt);
        Assert.Equal(FutureExpiry, activated.ExpiresAt);
        Assert.Equal(Parent, activated.DelegatorId);
    }

    [Fact]
    public void ActivateWithExpiry_PastDate_Throws()
    {
        var deleg = Delegation.Create(Id, Parent, Child);

        Assert.Throws<ArgumentException>(() => deleg.ActivateWithExpiry(PastExpiry));
    }

    [Fact]
    public void ActivateWithExpiry_ExactNow_Throws()
    {
        var deleg = Delegation.Create(Id, Parent, Child);
        // DateTimeOffset.UtcNow used inside the method; same-second value is <= now.
        Assert.Throws<ArgumentException>(() => deleg.ActivateWithExpiry(DateTimeOffset.UtcNow));
    }

    // ── Revoke ───────────────────────────────────────────────────────────────

    [Fact]
    public void Revoke_OnActive_SetsInactiveAndRaisesEvent()
    {
        var deleg = Delegation.Create(Id, Parent, Child);
        deleg.Activate();
        deleg.ClearDomainEvents();

        deleg.Revoke();

        Assert.False(deleg.IsActive);
        Assert.Null(deleg.ExpiresAt);
        var evt = Assert.Single(deleg.DomainEvents);
        var revoked = Assert.IsType<DelegationRevoked>(evt);
        Assert.Equal(Id, revoked.Id);
        Assert.Equal(Parent, revoked.DelegatorId);
    }

    [Fact]
    public void Revoke_OnInactive_Throws()
    {
        var deleg = Delegation.Create(Id, Parent, Child);

        Assert.Throws<InvalidOperationException>(() => deleg.Revoke());
    }

    [Fact]
    public void Revoke_AfterActivateWithExpiry_ClearsExpiry()
    {
        var deleg = Delegation.Create(Id, Parent, Child);
        deleg.ActivateWithExpiry(FutureExpiry);
        deleg.ClearDomainEvents();

        deleg.Revoke();

        Assert.False(deleg.IsActive);
        Assert.Null(deleg.ExpiresAt);
    }

    // ── Re-activate ──────────────────────────────────────────────────────────

    [Fact]
    public void Activate_AfterRevoke_ReactivatesSuccessfully()
    {
        var deleg = Delegation.Create(Id, Parent, Child);
        deleg.Activate();
        deleg.Revoke();
        deleg.ClearDomainEvents();

        deleg.Activate();

        Assert.True(deleg.IsActive);
        Assert.Single(deleg.DomainEvents);
    }
}
