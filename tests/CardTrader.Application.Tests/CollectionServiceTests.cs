using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Domain.Common;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using NSubstitute;

namespace CardTrader.Application.Tests;

public class CollectionServiceTests
{
    private readonly ICollectionRepository _collections = Substitute.For<ICollectionRepository>();
    private readonly IAuthorizationService _authz = Substitute.For<IAuthorizationService>();
    private readonly IDomainEventDispatcher _dispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly CollectionService _sut;

    public CollectionServiceTests()
    {
        _sut = new CollectionService(_collections, _authz, _dispatcher);
    }

    private static Collection MakeCollection(CollectionId id, UserId owner)
    {
        var c = Collection.Create(id, "Test Collection", owner);
        c.ClearDomainEvents();
        return c;
    }

    private void AllowManage() =>
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

    private void DenyManage() =>
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsAndDispatchesEvents()
    {
        var id = CollectionId.New();
        var owner = UserId.New();

        var result = await _sut.CreateAsync(id, "My Deck", owner);

        await _collections.Received().AddAsync(Arg.Is<Collection>(c => c.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task CreateAsync_ClearsEventsAfterDispatch()
    {
        var result = await _sut.CreateAsync(CollectionId.New(), "My Deck", UserId.New());

        Assert.Empty(result.DomainEvents);
    }

    // ── ShareWithUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ShareWithUserAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = CollectionId.New();
        var owner = UserId.New();
        _collections.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCollection(id, owner));
        AllowManage();

        await _sut.ShareWithUserAsync(id, owner, UserId.New());

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShareWithUserAsync_WhenUnauthorized_Throws()
    {
        var id = CollectionId.New();
        _collections.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCollection(id, UserId.New()));
        DenyManage();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ShareWithUserAsync(id, UserId.New(), UserId.New()));
    }

    [Fact]
    public async Task ShareWithUserAsync_WhenNotFound_Throws()
    {
        _collections.GetByIdAsync(Arg.Any<CollectionId>(), Arg.Any<CancellationToken>()).Returns((Collection?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.ShareWithUserAsync(CollectionId.New(), UserId.New(), UserId.New()));
    }

    // ── UnshareAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UnshareAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = CollectionId.New();
        var owner = UserId.New();
        _collections.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCollection(id, owner));
        AllowManage();

        await _sut.UnshareAsync(id, owner, UserId.New());

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnshareAsync_WhenUnauthorized_Throws()
    {
        var id = CollectionId.New();
        _collections.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCollection(id, UserId.New()));
        DenyManage();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UnshareAsync(id, UserId.New(), UserId.New()));
    }

    // ── MakePublicAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task MakePublicAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = CollectionId.New();
        var owner = UserId.New();
        _collections.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCollection(id, owner));
        AllowManage();

        await _sut.MakePublicAsync(id, owner);

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakePublicAsync_WhenUnauthorized_Throws()
    {
        var id = CollectionId.New();
        _collections.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCollection(id, UserId.New()));
        DenyManage();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.MakePublicAsync(id, UserId.New()));
    }

    // ── MakePrivateAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task MakePrivateAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = CollectionId.New();
        var owner = UserId.New();
        _collections.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCollection(id, owner));
        AllowManage();

        await _sut.MakePrivateAsync(id, owner);

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MakePrivateAsync_WhenUnauthorized_Throws()
    {
        var id = CollectionId.New();
        _collections.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCollection(id, UserId.New()));
        DenyManage();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.MakePrivateAsync(id, UserId.New()));
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_DelegatesToRepository()
    {
        var id = CollectionId.New();
        var expected = MakeCollection(id, UserId.New());
        _collections.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetByIdAsync(id);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _collections.GetByIdAsync(Arg.Any<CollectionId>(), Arg.Any<CancellationToken>()).Returns((Collection?)null);

        var result = await _sut.GetByIdAsync(CollectionId.New());

        Assert.Null(result);
    }

    // ── GetOwnedAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOwnedAsync_DelegatesToRepository()
    {
        var owner = UserId.New();
        var expected = new List<Collection> { MakeCollection(CollectionId.New(), owner) };
        _collections.GetAllByOwnerAsync(owner, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetOwnedAsync(owner);

        Assert.Equal(expected, result);
    }
}
