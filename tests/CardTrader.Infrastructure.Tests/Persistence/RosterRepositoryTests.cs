using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.Persistence;
using CardTrader.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Tests.Persistence;

public class RosterRepositoryTests
{
    private static AppDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var ctx = CreateContext(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
        var repo = new RosterRepository(ctx);

        var result = await repo.GetByIdAsync(RosterId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsRoster()
    {
        var dbName = nameof(AddAsync_PersistsRoster);
        var id = RosterId.New();
        var ownerId = UserId.New();
        var roster = Roster.Create(id, "Starting Lineup", ownerId);

        await using (var writeCtx = CreateContext(dbName))
            await new RosterRepository(writeCtx).AddAsync(roster);

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.Rosters.FirstOrDefaultAsync(r => r.Id == id);
        Assert.NotNull(stored);
        Assert.Equal("Starting Lineup", stored.Name);
        Assert.Equal(ownerId, stored.OwnerId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRoster_WhenFound()
    {
        var dbName = nameof(GetByIdAsync_ReturnsRoster_WhenFound);
        var id = RosterId.New();
        var ownerId = UserId.New();
        var roster = Roster.Create(id, "Dream Team", ownerId);

        await using (var writeCtx = CreateContext(dbName))
            await new RosterRepository(writeCtx).AddAsync(roster);

        await using var readCtx = CreateContext(dbName);
        var result = await new RosterRepository(readCtx).GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Dream Team", result.Name);
    }

    [Fact]
    public async Task GetAllByOwnerAsync_ReturnsOnlyOwnerRosters()
    {
        var dbName = nameof(GetAllByOwnerAsync_ReturnsOnlyOwnerRosters);
        var owner = UserId.New();
        var other = UserId.New();

        await using (var ctx = CreateContext(dbName))
        {
            var repo = new RosterRepository(ctx);
            await repo.AddAsync(Roster.Create(RosterId.New(), "Alice Lineup 1", owner));
            await repo.AddAsync(Roster.Create(RosterId.New(), "Alice Lineup 2", owner));
            await repo.AddAsync(Roster.Create(RosterId.New(), "Bob Lineup", other));
        }

        await using var readCtx = CreateContext(dbName);
        var results = await new RosterRepository(readCtx).GetAllByOwnerAsync(owner);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(owner, r.OwnerId));
    }
}
