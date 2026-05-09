using CardTrader.Application.Services;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using NSubstitute;

namespace CardTrader.Application.Tests;

public class CardServiceTests
{
    private readonly ICardRepository _cards = Substitute.For<ICardRepository>();
    private readonly CardService _sut;

    public CardServiceTests()
    {
        _sut = new CardService(_cards);
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
}
