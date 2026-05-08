using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Domain.Common;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using NSubstitute;

namespace CardTrader.Application.Tests;

public class CardInstanceServiceTests
{
    private readonly ICardInstanceRepository _instances = Substitute.For<ICardInstanceRepository>();
    private readonly IAuthorizationService _authz = Substitute.For<IAuthorizationService>();
    private readonly IDomainEventDispatcher _dispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly CardInstanceService _sut;

    public CardInstanceServiceTests()
    {
        _sut = new CardInstanceService(_instances, _authz, _dispatcher);
    }

    private static CardInstance MakeInstance(CardInstanceId id, UserId owner)
    {
        var i = CardInstance.Create(id, CardId.New(), owner, printNumber: 1);
        i.ClearDomainEvents();
        return i;
    }

    private void Allow() =>
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

    private void Deny() =>
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsAndDispatchesEvents()
    {
        var id = CardInstanceId.New();

        var result = await _sut.CreateAsync(id, CardId.New(), UserId.New(), printNumber: 42);

        await _instances.Received().AddAsync(Arg.Is<CardInstance>(i => i.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
        Assert.Equal(id, result.Id);
        Assert.Equal(42, result.PrintNumber);
    }

    [Fact]
    public async Task CreateAsync_ClearsEventsAfterDispatch()
    {
        var result = await _sut.CreateAsync(CardInstanceId.New(), CardId.New(), UserId.New(), printNumber: 1);

        Assert.Empty(result.DomainEvents);
    }

    // ── AddToCollectionAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task AddToCollectionAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = CardInstanceId.New();
        var owner = UserId.New();
        _instances.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeInstance(id, owner));
        Allow();

        await _sut.AddToCollectionAsync(id, owner, CollectionId.New());

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddToCollectionAsync_WhenUnauthorized_Throws()
    {
        var id = CardInstanceId.New();
        _instances.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeInstance(id, UserId.New()));
        Deny();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.AddToCollectionAsync(id, UserId.New(), CollectionId.New()));
    }

    [Fact]
    public async Task AddToCollectionAsync_WhenNotFound_Throws()
    {
        _instances.GetByIdAsync(Arg.Any<CardInstanceId>(), Arg.Any<CancellationToken>()).Returns((CardInstance?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AddToCollectionAsync(CardInstanceId.New(), UserId.New(), CollectionId.New()));
    }

    // ── RemoveFromCollectionAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RemoveFromCollectionAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = CardInstanceId.New();
        var owner = UserId.New();
        _instances.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeInstance(id, owner));
        Allow();

        await _sut.RemoveFromCollectionAsync(id, owner, CollectionId.New());

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveFromCollectionAsync_WhenUnauthorized_Throws()
    {
        var id = CardInstanceId.New();
        _instances.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeInstance(id, UserId.New()));
        Deny();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.RemoveFromCollectionAsync(id, UserId.New(), CollectionId.New()));
    }

    [Fact]
    public async Task RemoveFromCollectionAsync_WhenNotFound_Throws()
    {
        _instances.GetByIdAsync(Arg.Any<CardInstanceId>(), Arg.Any<CancellationToken>()).Returns((CardInstance?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.RemoveFromCollectionAsync(CardInstanceId.New(), UserId.New(), CollectionId.New()));
    }
}
