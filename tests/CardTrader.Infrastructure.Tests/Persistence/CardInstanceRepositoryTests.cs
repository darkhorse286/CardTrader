using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.Persistence;
using CardTrader.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Tests.Persistence;

public class CardInstanceRepositoryTests
{
    private static AppDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    private static Card SeedCard(AppDbContext ctx)
    {
        var card = Card.Create(CardId.New(), "Lightning Bolt", "Alpha", "Common",
            playerName: "Garfield", printRun: 1200);
        ctx.Cards.Add(card);
        ctx.SaveChanges();
        return card;
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var ctx = CreateContext(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
        var repo = new CardInstanceRepository(ctx);

        var result = await repo.GetByIdAsync(CardInstanceId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsCardInstance()
    {
        var dbName = nameof(AddAsync_PersistsCardInstance);
        CardId cardId;
        var instanceId = CardInstanceId.New();
        var ownerId = UserId.New();

        await using (var writeCtx = CreateContext(dbName))
        {
            var card = SeedCard(writeCtx);
            cardId = card.Id;
            var instance = CardInstance.Create(instanceId, cardId, ownerId, printNumber: 42);
            await new CardInstanceRepository(writeCtx).AddAsync(instance);
        }

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.CardInstances.FirstOrDefaultAsync(i => i.Id == instanceId);
        Assert.NotNull(stored);
        Assert.Equal(cardId, stored.CardId);
        Assert.Equal(ownerId, stored.OwnerId);
        Assert.Equal(42, stored.PrintNumber);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsInstance_WhenFound()
    {
        var dbName = nameof(GetByIdAsync_ReturnsInstance_WhenFound);
        var instanceId = CardInstanceId.New();
        var ownerId = UserId.New();

        await using (var writeCtx = CreateContext(dbName))
        {
            var card = SeedCard(writeCtx);
            var instance = CardInstance.Create(instanceId, card.Id, ownerId, printNumber: 7);
            await new CardInstanceRepository(writeCtx).AddAsync(instance);
        }

        await using var readCtx = CreateContext(dbName);
        var result = await new CardInstanceRepository(readCtx).GetByIdAsync(instanceId);

        Assert.NotNull(result);
        Assert.Equal(instanceId, result.Id);
        Assert.Equal(ownerId, result.OwnerId);
        Assert.Equal(7, result.PrintNumber);
    }
}
