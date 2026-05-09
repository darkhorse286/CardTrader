using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Domain;
using CardTrader.Domain.Common;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using NSubstitute;

namespace CardTrader.Application.Tests;

public class CardInstanceServiceTests
{
    private readonly ICardInstanceRepository _instances = Substitute.For<ICardInstanceRepository>();
    private readonly ICardRepository _cards = Substitute.For<ICardRepository>();
    private readonly IAuthorizationService _authz = Substitute.For<IAuthorizationService>();
    private readonly IDomainEventDispatcher _dispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly CardInstanceService _sut;

    private static readonly Card DefaultCard =
        Card.Create(CardId.New(), "Test Card", "Test Set", "Common", "Test Player", printRun: 100);

    public CardInstanceServiceTests()
    {
        _sut = new CardInstanceService(_instances, _cards, _authz, _dispatcher);
        _instances.GetByRosterAsync(Arg.Any<RosterId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CardInstance>)[]);
        _instances.ExistsByCardAndPrintNumberAsync(Arg.Any<CardId>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _cards.GetByIdAsync(Arg.Any<CardId>(), Arg.Any<CancellationToken>())
            .Returns(DefaultCard);
    }

    private void SimulateDuplicatePrintNumber() =>
        _instances.ExistsByCardAndPrintNumberAsync(Arg.Any<CardId>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

    private static CardInstance MakeInstance(CardInstanceId id, UserId owner)
    {
        var i = CardInstance.Create(id, CardId.New(), owner, printNumber: 1, printRun: 100);
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

    [Fact]
    public async Task CreateAsync_WhenDuplicatePrintNumber_Throws()
    {
        SimulateDuplicatePrintNumber();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateAsync(CardInstanceId.New(), CardId.New(), UserId.New(), printNumber: 1));
    }

    [Fact]
    public async Task CreateAsync_WhenCardNotFound_Throws()
    {
        _cards.GetByIdAsync(Arg.Any<CardId>(), Arg.Any<CancellationToken>()).Returns((Card?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.CreateAsync(CardInstanceId.New(), CardId.New(), UserId.New(), printNumber: 1));
    }

    [Fact]
    public async Task CreateAsync_WhenPrintNumberExceedsPrintRun_Throws()
    {
        _cards.GetByIdAsync(Arg.Any<CardId>(), Arg.Any<CancellationToken>())
            .Returns(Card.Create(CardId.New(), "Test", "Set", "Common", "Player", printRun: 5));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.CreateAsync(CardInstanceId.New(), CardId.New(), UserId.New(), printNumber: 6));
    }

    // ── AddToRosterAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddToRosterAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = CardInstanceId.New();
        var owner = UserId.New();
        _instances.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeInstance(id, owner));
        Allow();

        await _sut.AddToRosterAsync(id, owner, RosterId.New());

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddToRosterAsync_WhenUnauthorized_Throws()
    {
        var id = CardInstanceId.New();
        _instances.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeInstance(id, UserId.New()));
        Deny();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.AddToRosterAsync(id, UserId.New(), RosterId.New()));
    }

    [Fact]
    public async Task AddToRosterAsync_WhenNotFound_Throws()
    {
        _instances.GetByIdAsync(Arg.Any<CardInstanceId>(), Arg.Any<CancellationToken>()).Returns((CardInstance?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AddToRosterAsync(CardInstanceId.New(), UserId.New(), RosterId.New()));
    }

    // ── RemoveFromRosterAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveFromRosterAsync_WhenAuthorized_DispatchesEvent()
    {
        var id = CardInstanceId.New();
        var owner = UserId.New();
        _instances.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeInstance(id, owner));
        Allow();

        await _sut.RemoveFromRosterAsync(id, owner, RosterId.New());

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveFromRosterAsync_WhenUnauthorized_Throws()
    {
        var id = CardInstanceId.New();
        _instances.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeInstance(id, UserId.New()));
        Deny();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.RemoveFromRosterAsync(id, UserId.New(), RosterId.New()));
    }

    [Fact]
    public async Task RemoveFromRosterAsync_WhenNotFound_Throws()
    {
        _instances.GetByIdAsync(Arg.Any<CardInstanceId>(), Arg.Any<CancellationToken>()).Returns((CardInstance?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.RemoveFromRosterAsync(CardInstanceId.New(), UserId.New(), RosterId.New()));
    }

    // ── MintAndAddToRosterAsync ───────────────────────────────────────────────

    [Fact]
    public async Task MintAndAddToRosterAsync_PersistsAndDispatchesEvents()
    {
        var id = CardInstanceId.New();
        var rosterId = RosterId.New();

        var result = await _sut.MintAndAddToRosterAsync(id, CardId.New(), UserId.New(), 1, rosterId);

        await _instances.Received().AddAsync(Arg.Is<CardInstance>(i => i.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task MintAndAddToRosterAsync_SetsRosterId()
    {
        var rosterId = RosterId.New();

        var result = await _sut.MintAndAddToRosterAsync(
            CardInstanceId.New(), CardId.New(), UserId.New(), 1, rosterId);

        Assert.Equal(rosterId, result.RosterId);
    }

    [Fact]
    public async Task MintAndAddToRosterAsync_ClearsEventsAfterDispatch()
    {
        var result = await _sut.MintAndAddToRosterAsync(
            CardInstanceId.New(), CardId.New(), UserId.New(), 1, RosterId.New());

        Assert.Empty(result.DomainEvents);
    }

    [Fact]
    public async Task MintAndAddToRosterAsync_WhenDuplicatePrintNumber_Throws()
    {
        SimulateDuplicatePrintNumber();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.MintAndAddToRosterAsync(CardInstanceId.New(), CardId.New(), UserId.New(), 1, RosterId.New()));
    }

    [Fact]
    public async Task MintAndAddToRosterAsync_WhenCardNotFound_Throws()
    {
        _cards.GetByIdAsync(Arg.Any<CardId>(), Arg.Any<CancellationToken>()).Returns((Card?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.MintAndAddToRosterAsync(CardInstanceId.New(), CardId.New(), UserId.New(), 1, RosterId.New()));
    }

    [Fact]
    public async Task MintAndAddToRosterAsync_WhenPrintNumberExceedsPrintRun_Throws()
    {
        _cards.GetByIdAsync(Arg.Any<CardId>(), Arg.Any<CancellationToken>())
            .Returns(Card.Create(CardId.New(), "Test", "Set", "Common", "Player", printRun: 5));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.MintAndAddToRosterAsync(CardInstanceId.New(), CardId.New(), UserId.New(), 6, RosterId.New()));
    }

    [Fact]
    public async Task MintAndAddToRosterAsync_WhenRosterFull_Throws()
    {
        var rosterId = RosterId.New();
        var full = Enumerable.Range(0, RosterConstraints.MaxSize)
            .Select(_ => MakeInstance(CardInstanceId.New(), UserId.New()))
            .ToList<CardInstance>();
        _instances.GetByRosterAsync(rosterId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CardInstance>)full);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.MintAndAddToRosterAsync(CardInstanceId.New(), CardId.New(), UserId.New(), 1, rosterId));
    }

    [Fact]
    public async Task MintAndAddToRosterAsync_WhenPositionSlotFull_Throws()
    {
        var attackCard = Card.Create(CardId.New(), "Attacker", "Set", "Rare", "P", 100,
            playerPosition: "Attack");
        var rosterId = RosterId.New();

        var existingInstances = Enumerable.Range(0, RosterConstraints.PositionSlots["Attack"])
            .Select(_ => MakeInstance(CardInstanceId.New(), UserId.New()))
            .ToList<CardInstance>();
        _instances.GetByRosterAsync(rosterId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CardInstance>)existingInstances);
        _cards.GetByIdAsync(Arg.Any<CardId>(), Arg.Any<CancellationToken>()).Returns(attackCard);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.MintAndAddToRosterAsync(CardInstanceId.New(), attackCard.Id, UserId.New(), 1, rosterId));
    }

    // ── GetByRosterAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByRosterAsync_DelegatesToRepository()
    {
        var rosterId = RosterId.New();
        var expected = new List<CardInstance> { MakeInstance(CardInstanceId.New(), UserId.New()) };
        _instances.GetByRosterAsync(rosterId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetByRosterAsync(rosterId);

        Assert.Equal(expected, result);
    }

    // ── AddToRosterAsync (UpdateAsync) ────────────────────────────────────────

    [Fact]
    public async Task AddToRosterAsync_WhenAuthorized_CallsUpdateAsync()
    {
        var id = CardInstanceId.New();
        _instances.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeInstance(id, UserId.New()));
        Allow();

        await _sut.AddToRosterAsync(id, UserId.New(), RosterId.New());

        await _instances.Received().UpdateAsync(Arg.Any<CardInstance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveFromRosterAsync_WhenAuthorized_CallsUpdateAsync()
    {
        var id = CardInstanceId.New();
        _instances.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeInstance(id, UserId.New()));
        Allow();

        await _sut.RemoveFromRosterAsync(id, UserId.New(), RosterId.New());

        await _instances.Received().UpdateAsync(Arg.Any<CardInstance>(), Arg.Any<CancellationToken>());
    }
}
