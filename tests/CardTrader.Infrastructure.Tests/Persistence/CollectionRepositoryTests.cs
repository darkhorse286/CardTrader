using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.Persistence;
using CardTrader.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Tests.Persistence;

public class CollectionRepositoryTests
{
    private static AppDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var ctx = CreateContext(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
        var repo = new CollectionRepository(ctx);

        var result = await repo.GetByIdAsync(CollectionId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsCollection()
    {
        var dbName = nameof(AddAsync_PersistsCollection);
        var id = CollectionId.New();
        var ownerId = UserId.New();
        var collection = Collection.Create(id, "My Deck", ownerId);

        await using (var writeCtx = CreateContext(dbName))
            await new CollectionRepository(writeCtx).AddAsync(collection);

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.Collections.FirstOrDefaultAsync(c => c.Id == id);
        Assert.NotNull(stored);
        Assert.Equal("My Deck", stored.Name);
        Assert.Equal(ownerId, stored.OwnerId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCollection_WhenFound()
    {
        var dbName = nameof(GetByIdAsync_ReturnsCollection_WhenFound);
        var id = CollectionId.New();
        var ownerId = UserId.New();
        var collection = Collection.Create(id, "Trade Binder", ownerId);

        await using (var writeCtx = CreateContext(dbName))
            await new CollectionRepository(writeCtx).AddAsync(collection);

        await using var readCtx = CreateContext(dbName);
        var result = await new CollectionRepository(readCtx).GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Trade Binder", result.Name);
    }
}
