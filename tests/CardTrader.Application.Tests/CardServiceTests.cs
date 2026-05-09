using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using NSubstitute;

namespace CardTrader.Application.Tests;

public class CardServiceTests
{
    private readonly ICardRepository _cards = Substitute.For<ICardRepository>();
    private readonly IAdminService _admin = Substitute.For<IAdminService>();
    private readonly CardService _sut;

    public CardServiceTests()
    {
        _sut = new CardService(_cards, _admin);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        var expected = new List<Card>
        {
            Card.Create(CardId.New(), "Lightning Bolt", "Alpha", "Common", "Garfield", 1000)
        };
        _cards.GetAllAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetAllAsync();

        Assert.Equal(expected, result);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_WhenAdmin_PersistsCard()
    {
        var adminId = UserId.New();
        _admin.IsAdminAsync(adminId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.AddAsync(
            CardId.New(), "Jordan Retirement", "Legends", "Legendary", "Michael Jordan",
            23, adminId, playerPosition: "Attack");

        await _cards.Received().AddAsync(Arg.Is<Card>(c => c.Name == "Jordan Retirement"), Arg.Any<CancellationToken>());
        Assert.Equal("Jordan Retirement", result.Name);
    }

    [Fact]
    public async Task AddAsync_WhenNotAdmin_Throws()
    {
        _admin.IsAdminAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.AddAsync(CardId.New(), "Card", "Set", "Rare", "Player", 100, UserId.New()));
    }
}
