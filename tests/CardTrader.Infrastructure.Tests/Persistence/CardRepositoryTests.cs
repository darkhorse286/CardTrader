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

    private static Card MakeCard(string name = "Black Lotus") =>
        Card.Create(CardId.New(), name, "Alpha", "Rare", playerName: "Garfield", printRun: 100);

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
        var card = Card.Create(CardId.New(), "Black Lotus", "Alpha", "Rare",
            playerName: "Garfield", printRun: 10,
            playerPosition: "Midfield", teamName: "Arsenal",
            imageUrl: "https://example.com/img.png");

        await using (var writeCtx = CreateContext(dbName))
            await new CardRepository(writeCtx).AddAsync(card);

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.Cards.FirstOrDefaultAsync(c => c.Id == card.Id);
        Assert.NotNull(stored);
        Assert.Equal("Black Lotus", stored.Name);
        Assert.Equal("Alpha", stored.SetName);
        Assert.Equal("Rare", stored.Rarity);
        Assert.Equal("Garfield", stored.PlayerName);
        Assert.Equal(10, stored.PrintRun);
        Assert.Equal("Midfield", stored.PlayerPosition);
        Assert.Equal("Arsenal", stored.TeamName);
        Assert.Equal("https://example.com/img.png", stored.ImageUrl);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCard_WhenFound()
    {
        var dbName = nameof(GetByIdAsync_ReturnsCard_WhenFound);
        var card = MakeCard("Counterspell");

        await using (var writeCtx = CreateContext(dbName))
            await new CardRepository(writeCtx).AddAsync(card);

        await using var readCtx = CreateContext(dbName);
        var result = await new CardRepository(readCtx).GetByIdAsync(card.Id);

        Assert.NotNull(result);
        Assert.Equal(card.Id, result.Id);
        Assert.Equal("Counterspell", result.Name);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCards_OrderedByName()
    {
        var dbName = nameof(GetAllAsync_ReturnsAllCards_OrderedByName);

        await using (var ctx = CreateContext(dbName))
        {
            var repo = new CardRepository(ctx);
            await repo.AddAsync(MakeCard("Zombie"));
            await repo.AddAsync(MakeCard("Angel"));
            await repo.AddAsync(MakeCard("Merfolk"));
        }

        await using var readCtx = CreateContext(dbName);
        var results = await new CardRepository(readCtx).GetAllAsync();

        Assert.Equal(3, results.Count);
        Assert.Equal("Angel", results[0].Name);
        Assert.Equal("Merfolk", results[1].Name);
        Assert.Equal("Zombie", results[2].Name);
    }
}
