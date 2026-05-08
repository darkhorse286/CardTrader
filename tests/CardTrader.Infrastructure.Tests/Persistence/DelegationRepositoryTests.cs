using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.Persistence;
using CardTrader.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Tests.Persistence;

public class DelegationRepositoryTests
{
    private static AppDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var ctx = CreateContext(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
        var repo = new DelegationRepository(ctx);

        var result = await repo.GetByIdAsync(DelegationId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsDelegation()
    {
        var dbName = nameof(AddAsync_PersistsDelegation);
        var id = DelegationId.New();
        var delegatorId = UserId.New();
        var delegateeId = UserId.New();
        var delegation = Delegation.Create(id, delegatorId, delegateeId);

        await using (var writeCtx = CreateContext(dbName))
            await new DelegationRepository(writeCtx).AddAsync(delegation);

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.Delegations.FirstOrDefaultAsync(d => d.Id == id);
        Assert.NotNull(stored);
        Assert.Equal(delegatorId, stored.DelegatorId);
        Assert.Equal(delegateeId, stored.DelegateeId);
        Assert.False(stored.IsActive);
        Assert.Null(stored.ExpiresAt);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDelegation_WhenFound()
    {
        var dbName = nameof(GetByIdAsync_ReturnsDelegation_WhenFound);
        var id = DelegationId.New();
        var delegation = Delegation.Create(id, UserId.New(), UserId.New());

        await using (var writeCtx = CreateContext(dbName))
            await new DelegationRepository(writeCtx).AddAsync(delegation);

        await using var readCtx = CreateContext(dbName);
        var result = await new DelegationRepository(readCtx).GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_PersistsActivation()
    {
        var dbName = nameof(UpdateAsync_PersistsActivation);
        var id = DelegationId.New();

        await using (var writeCtx = CreateContext(dbName))
        {
            var delegation = Delegation.Create(id, UserId.New(), UserId.New());
            await new DelegationRepository(writeCtx).AddAsync(delegation);
        }

        await using (var updateCtx = CreateContext(dbName))
        {
            var delegation = await new DelegationRepository(updateCtx).GetByIdAsync(id);
            delegation!.Activate();
            await new DelegationRepository(updateCtx).UpdateAsync(delegation);
        }

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.Delegations.FirstOrDefaultAsync(d => d.Id == id);
        Assert.NotNull(stored);
        Assert.True(stored.IsActive);
        Assert.Null(stored.ExpiresAt);
    }

    [Fact]
    public async Task UpdateAsync_PersistsActivationWithExpiry()
    {
        var dbName = nameof(UpdateAsync_PersistsActivationWithExpiry);
        var id = DelegationId.New();
        var expiry = DateTimeOffset.UtcNow.AddDays(30);

        await using (var writeCtx = CreateContext(dbName))
        {
            var delegation = Delegation.Create(id, UserId.New(), UserId.New());
            await new DelegationRepository(writeCtx).AddAsync(delegation);
        }

        await using (var updateCtx = CreateContext(dbName))
        {
            var delegation = await new DelegationRepository(updateCtx).GetByIdAsync(id);
            delegation!.ActivateWithExpiry(expiry);
            await new DelegationRepository(updateCtx).UpdateAsync(delegation);
        }

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.Delegations.FirstOrDefaultAsync(d => d.Id == id);
        Assert.NotNull(stored);
        Assert.True(stored.IsActive);
        Assert.NotNull(stored.ExpiresAt);
    }
}
