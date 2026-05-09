using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Domain.Common;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using NSubstitute;

namespace CardTrader.Application.Tests;

public class RosterServiceTests
{
    private readonly IRosterRepository _rosters = Substitute.For<IRosterRepository>();
    private readonly IAuthorizationService _authz = Substitute.For<IAuthorizationService>();
    private readonly IDomainEventDispatcher _dispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly RosterService _sut;

    public RosterServiceTests()
    {
        _sut = new RosterService(_rosters, _authz, _dispatcher);
    }

    private static Roster MakeRoster(RosterId id, UserId owner)
    {
        var r = Roster.Create(id, "Test Roster", owner);
        r.ClearDomainEvents();
        return r;
    }

    private void AllowManage() =>
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

    private void DenyManage() =>
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsAndDispatchesEvents()
    {
        var id = RosterId.New();
        var owner = UserId.New();

        var result = await _sut.CreateAsync(id, "My Lineup", owner);

        await _rosters.Received().AddAsync(Arg.Is<Roster>(r => r.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task CreateAsync_ClearsEventsAfterDispatch()
    {
        var result = await _sut.CreateAsync(RosterId.New(), "My Lineup", UserId.New());

        Assert.Empty(result.DomainEvents);
    }

    // ── ShareWithUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ShareWithUserAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = RosterId.New();
        var owner = UserId.New();
        _rosters.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeRoster(id, owner));
        AllowManage();

        await _sut.ShareWithUserAsync(id, owner, UserId.New());

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShareWithUserAsync_WhenUnauthorized_Throws()
    {
        var id = RosterId.New();
        _rosters.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeRoster(id, UserId.New()));
        DenyManage();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ShareWithUserAsync(id, UserId.New(), UserId.New()));
    }

    [Fact]
    public async Task ShareWithUserAsync_WhenNotFound_Throws()
    {
        _rosters.GetByIdAsync(Arg.Any<RosterId>(), Arg.Any<CancellationToken>()).Returns((Roster?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.ShareWithUserAsync(RosterId.New(), UserId.New(), UserId.New()));
    }

    // ── UnshareAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UnshareAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = RosterId.New();
        var owner = UserId.New();
        _rosters.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeRoster(id, owner));
        AllowManage();

        await _sut.UnshareAsync(id, owner, UserId.New());

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnshareAsync_WhenUnauthorized_Throws()
    {
        var id = RosterId.New();
        _rosters.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeRoster(id, UserId.New()));
        DenyManage();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UnshareAsync(id, UserId.New(), UserId.New()));
    }

    // ── MakePublicAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task MakePublicAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = RosterId.New();
        var owner = UserId.New();
        _rosters.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeRoster(id, owner));
        AllowManage();

        await _sut.MakePublicAsync(id, owner);

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakePublicAsync_WhenUnauthorized_Throws()
    {
        var id = RosterId.New();
        _rosters.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeRoster(id, UserId.New()));
        DenyManage();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.MakePublicAsync(id, UserId.New()));
    }

    // ── MakePrivateAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task MakePrivateAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = RosterId.New();
        var owner = UserId.New();
        _rosters.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeRoster(id, owner));
        AllowManage();

        await _sut.MakePrivateAsync(id, owner);

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakePrivateAsync_WhenUnauthorized_Throws()
    {
        var id = RosterId.New();
        _rosters.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeRoster(id, UserId.New()));
        DenyManage();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.MakePrivateAsync(id, UserId.New()));
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_DelegatesToRepository()
    {
        var id = RosterId.New();
        var expected = MakeRoster(id, UserId.New());
        _rosters.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetByIdAsync(id);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _rosters.GetByIdAsync(Arg.Any<RosterId>(), Arg.Any<CancellationToken>()).Returns((Roster?)null);

        var result = await _sut.GetByIdAsync(RosterId.New());

        Assert.Null(result);
    }

    // ── GetOwnedAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOwnedAsync_DelegatesToRepository()
    {
        var owner = UserId.New();
        var expected = new List<Roster> { MakeRoster(RosterId.New(), owner) };
        _rosters.GetAllByOwnerAsync(owner, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetOwnedAsync(owner);

        Assert.Equal(expected, result);
    }
}
