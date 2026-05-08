using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.Persistence;
using CardTrader.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Tests.Persistence;

public class CardRepositoryTests
{
    private static AppDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var ctx = CreateContext(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
        var repo = new CardRepository(ctx);

        var result = await repo.GetByIdAsync(CardId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsCard()
    {
        var dbName = nameof(AddAsync_PersistsCard);
        var card = Card.Create(CardId.New(), "Black Lotus", "Alpha", "Rare");

        await using (var writeCtx = CreateContext(dbName))
            await new CardRepository(writeCtx).AddAsync(card);

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.Cards.FirstOrDefaultAsync(c => c.Id == card.Id);
        Assert.NotNull(stored);
        Assert.Equal("Black Lotus", stored.Name);
        Assert.Equal("Alpha", stored.SetName);
        Assert.Equal("Rare", stored.Rarity);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCard_WhenFound()
    {
        var dbName = nameof(GetByIdAsync_ReturnsCard_WhenFound);
        var card = Card.Create(CardId.New(), "Counterspell", "Beta", "Uncommon");

        await using (var writeCtx = CreateContext(dbName))
            await new CardRepository(writeCtx).AddAsync(card);

        await using var readCtx = CreateContext(dbName);
        var result = await new CardRepository(readCtx).GetByIdAsync(card.Id);

        Assert.NotNull(result);
        Assert.Equal(card.Id, result.Id);
        Assert.Equal("Counterspell", result.Name);
    }
}
