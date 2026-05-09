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

    [Fact]
    public async Task GetByRosterAsync_ReturnsOnlyInstancesInRoster()
    {
        var dbName = nameof(GetByRosterAsync_ReturnsOnlyInstancesInRoster);
        var rosterId = RosterId.New();
        var otherId = RosterId.New();

        await using (var ctx = CreateContext(dbName))
        {
            var card = SeedCard(ctx);
            var owner = UserId.New();

            var inRoster = CardInstance.Create(CardInstanceId.New(), card.Id, owner, 1);
            inRoster.AddToRoster(rosterId);

            var alsoIn = CardInstance.Create(CardInstanceId.New(), card.Id, owner, 2);
            alsoIn.AddToRoster(rosterId);

            var notIn = CardInstance.Create(CardInstanceId.New(), card.Id, owner, 3);
            notIn.AddToRoster(otherId);

            var repo = new CardInstanceRepository(ctx);
            await repo.AddAsync(inRoster);
            await repo.AddAsync(alsoIn);
            await repo.AddAsync(notIn);
        }

        await using var readCtx = CreateContext(dbName);
        var results = await new CardInstanceRepository(readCtx).GetByRosterAsync(rosterId);

        Assert.Equal(2, results.Count);
        Assert.All(results, i => Assert.Equal(rosterId, i.RosterId));
    }

    [Fact]
    public async Task ExistsByCardAndPrintNumberAsync_ReturnsFalse_WhenNoneExist()
    {
        await using var ctx = CreateContext(nameof(ExistsByCardAndPrintNumberAsync_ReturnsFalse_WhenNoneExist));
        var card = SeedCard(ctx);

        var result = await new CardInstanceRepository(ctx).ExistsByCardAndPrintNumberAsync(card.Id, 1);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByCardAndPrintNumberAsync_ReturnsTrue_WhenMatchExists()
    {
        var dbName = nameof(ExistsByCardAndPrintNumberAsync_ReturnsTrue_WhenMatchExists);

        await using (var ctx = CreateContext(dbName))
        {
            var card = SeedCard(ctx);
            var instance = CardInstance.Create(CardInstanceId.New(), card.Id, UserId.New(), printNumber: 5);
            await new CardInstanceRepository(ctx).AddAsync(instance);
        }

        await using var readCtx = CreateContext(dbName);
        var card2 = await readCtx.Cards.FirstAsync();
        var result = await new CardInstanceRepository(readCtx).ExistsByCardAndPrintNumberAsync(card2.Id, 5);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByCardAndPrintNumberAsync_ReturnsFalse_WhenDifferentCard()
    {
        var dbName = nameof(ExistsByCardAndPrintNumberAsync_ReturnsFalse_WhenDifferentCard);

        await using (var ctx = CreateContext(dbName))
        {
            var card = SeedCard(ctx);
            var instance = CardInstance.Create(CardInstanceId.New(), card.Id, UserId.New(), printNumber: 5);
            await new CardInstanceRepository(ctx).AddAsync(instance);
        }

        await using var readCtx = CreateContext(dbName);
        var result = await new CardInstanceRepository(readCtx).ExistsByCardAndPrintNumberAsync(CardId.New(), 5);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsRosterIdChange()
    {
        var dbName = nameof(UpdateAsync_PersistsRosterIdChange);
        var instanceId = CardInstanceId.New();
        var rosterId = RosterId.New();

        await using (var ctx = CreateContext(dbName))
        {
            var card = SeedCard(ctx);
            var instance = CardInstance.Create(instanceId, card.Id, UserId.New(), 1);
            await new CardInstanceRepository(ctx).AddAsync(instance);
        }

        await using (var ctx = CreateContext(dbName))
        {
            var repo = new CardInstanceRepository(ctx);
            var instance = await repo.GetByIdAsync(instanceId);
            instance!.AddToRoster(rosterId);
            await repo.UpdateAsync(instance);
        }

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.CardInstances.FindAsync(instanceId);
        Assert.Equal(rosterId, stored!.RosterId);
    }
}
