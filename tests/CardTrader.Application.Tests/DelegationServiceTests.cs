using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Domain.Common;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using NSubstitute;

namespace CardTrader.Application.Tests;

public class DelegationServiceTests
{
    private readonly IDelegationRepository _delegations = Substitute.For<IDelegationRepository>();
    private readonly IAuthorizationService _authz = Substitute.For<IAuthorizationService>();
    private readonly IDomainEventDispatcher _dispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly DelegationService _sut;

    public DelegationServiceTests()
    {
        _sut = new DelegationService(_delegations, _authz, _dispatcher);
    }

    private static Delegation MakeDelegation(DelegationId id, UserId delegator, UserId delegatee)
    {
        var d = Delegation.Create(id, delegator, delegatee);
        d.ClearDomainEvents();
        return d;
    }

    private void AllowDelegator() =>
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

    private void DenyDelegator() =>
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsAndDispatchesEvents()
    {
        var id = DelegationId.New();
        var delegator = UserId.New();
        var delegatee = UserId.New();

        var result = await _sut.CreateAsync(id, delegator, delegatee);

        await _delegations.Received().AddAsync(Arg.Is<Delegation>(d => d.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task CreateAsync_ClearsEventsAfterDispatch()
    {
        var result = await _sut.CreateAsync(DelegationId.New(), UserId.New(), UserId.New());

        Assert.Empty(result.DomainEvents);
    }

    // ── ActivateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_WhenAuthorized_UpdatesAndDispatchesEvent()
    {
        var id = DelegationId.New();
        var delegator = UserId.New();
        var delegation = MakeDelegation(id, delegator, UserId.New());
        _delegations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(delegation);
        AllowDelegator();

        await _sut.ActivateAsync(id, delegator);

        await _delegations.Received().UpdateAsync(Arg.Is<Delegation>(d => d.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateAsync_WhenUnauthorized_Throws()
    {
        var id = DelegationId.New();
        _delegations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeDelegation(id, UserId.New(), UserId.New()));
        DenyDelegator();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.ActivateAsync(id, UserId.New()));
    }

    [Fact]
    public async Task ActivateAsync_WhenNotFound_Throws()
    {
        _delegations.GetByIdAsync(Arg.Any<DelegationId>(), Arg.Any<CancellationToken>()).Returns((Delegation?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ActivateAsync(DelegationId.New(), UserId.New()));
    }

    // ── ActivateWithExpiryAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ActivateWithExpiryAsync_WhenAuthorized_UpdatesAndDispatchesEvent()
    {
        var id = DelegationId.New();
        var delegator = UserId.New();
        var delegation = MakeDelegation(id, delegator, UserId.New());
        _delegations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(delegation);
        AllowDelegator();

        await _sut.ActivateWithExpiryAsync(id, delegator, DateTimeOffset.UtcNow.AddDays(30));

        await _delegations.Received().UpdateAsync(Arg.Is<Delegation>(d => d.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateWithExpiryAsync_WhenUnauthorized_Throws()
    {
        var id = DelegationId.New();
        _delegations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeDelegation(id, UserId.New(), UserId.New()));
        DenyDelegator();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ActivateWithExpiryAsync(id, UserId.New(), DateTimeOffset.UtcNow.AddDays(30)));
    }

    // ── RevokeAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAsync_WhenAuthorized_UpdatesAndDispatchesEvent()
    {
        var id = DelegationId.New();
        var delegator = UserId.New();
        var delegation = MakeDelegation(id, delegator, UserId.New());
        delegation.Activate();
        delegation.ClearDomainEvents();
        _delegations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(delegation);
        AllowDelegator();

        await _sut.RevokeAsync(id, delegator);

        await _delegations.Received().UpdateAsync(Arg.Is<Delegation>(d => d.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeAsync_WhenUnauthorized_Throws()
    {
        var id = DelegationId.New();
        var delegation = MakeDelegation(id, UserId.New(), UserId.New());
        delegation.Activate();
        delegation.ClearDomainEvents();
        _delegations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(delegation);
        DenyDelegator();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RevokeAsync(id, UserId.New()));
    }

    [Fact]
    public async Task RevokeAsync_WhenNotFound_Throws()
    {
        _delegations.GetByIdAsync(Arg.Any<DelegationId>(), Arg.Any<CancellationToken>()).Returns((Delegation?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.RevokeAsync(DelegationId.New(), UserId.New()));
    }

    // ── GetOutgoingAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOutgoingAsync_ReturnsRepositoryResult()
    {
        var delegatorId = UserId.New();
        var delegation = MakeDelegation(DelegationId.New(), delegatorId, UserId.New());
        _delegations.GetByDelegatorAsync(delegatorId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Delegation>>([delegation]));

        var result = await _sut.GetOutgoingAsync(delegatorId);

        Assert.Single(result);
        Assert.Equal(delegation.Id, result[0].Id);
    }

    // ── GetIncomingAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetIncomingAsync_ReturnsRepositoryResult()
    {
        var delegateeId = UserId.New();
        var delegation = MakeDelegation(DelegationId.New(), UserId.New(), delegateeId);
        _delegations.GetByDelegateeAsync(delegateeId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Delegation>>([delegation]));

        var result = await _sut.GetIncomingAsync(delegateeId);

        Assert.Single(result);
        Assert.Equal(delegation.Id, result[0].Id);
    }
}
