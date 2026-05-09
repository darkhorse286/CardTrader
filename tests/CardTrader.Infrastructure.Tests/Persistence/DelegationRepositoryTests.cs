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

    [Fact]
    public async Task GetByDelegatorAsync_IncludesDelegationsWithFutureExpiry()
    {
        var dbName = nameof(GetByDelegatorAsync_IncludesDelegationsWithFutureExpiry);
        var delegatorId = UserId.New();
        var d = Delegation.Create(DelegationId.New(), delegatorId, UserId.New());
        d.ActivateWithExpiry(DateTimeOffset.UtcNow.AddDays(30));

        await using (var ctx = CreateContext(dbName))
            await new DelegationRepository(ctx).AddAsync(d);

        await using var readCtx = CreateContext(dbName);
        var result = await new DelegationRepository(readCtx).GetByDelegatorAsync(delegatorId);

        Assert.Single(result);
        Assert.NotNull(result[0].ExpiresAt);
    }

    [Fact]
    public async Task GetByDelegatorAsync_ExcludesExpiredActiveDelegations()
    {
        var dbName = nameof(GetByDelegatorAsync_ExcludesExpiredActiveDelegations);
        var delegatorId = UserId.New();

        var active = Delegation.Create(DelegationId.New(), delegatorId, UserId.New());
        active.Activate();

        var expiring = Delegation.Create(DelegationId.New(), delegatorId, UserId.New());
        expiring.ActivateWithExpiry(DateTimeOffset.UtcNow.AddMilliseconds(80));

        await using (var ctx = CreateContext(dbName))
        {
            var repo = new DelegationRepository(ctx);
            await repo.AddAsync(active);
            await repo.AddAsync(expiring);
        }

        await Task.Delay(120);

        await using var readCtx = CreateContext(dbName);
        var result = await new DelegationRepository(readCtx).GetByDelegatorAsync(delegatorId);

        Assert.Single(result);
        Assert.Null(result[0].ExpiresAt);
    }

    [Fact]
    public async Task GetByDelegateeAsync_ExcludesExpiredActiveDelegations()
    {
        var dbName = nameof(GetByDelegateeAsync_ExcludesExpiredActiveDelegations);
        var delegateeId = UserId.New();

        var active = Delegation.Create(DelegationId.New(), UserId.New(), delegateeId);
        active.Activate();

        var expiring = Delegation.Create(DelegationId.New(), UserId.New(), delegateeId);
        expiring.ActivateWithExpiry(DateTimeOffset.UtcNow.AddMilliseconds(80));

        await using (var ctx = CreateContext(dbName))
        {
            var repo = new DelegationRepository(ctx);
            await repo.AddAsync(active);
            await repo.AddAsync(expiring);
        }

        await Task.Delay(120);

        await using var readCtx = CreateContext(dbName);
        var result = await new DelegationRepository(readCtx).GetByDelegateeAsync(delegateeId);

        Assert.Single(result);
        Assert.Null(result[0].ExpiresAt);
    }

    [Fact]
    public async Task GetByDelegatorAsync_ReturnsOnlyDelegatorsDelegations()
    {
        var dbName = nameof(GetByDelegatorAsync_ReturnsOnlyDelegatorsDelegations);
        var delegatorId = UserId.New();
        var d1 = Delegation.Create(DelegationId.New(), delegatorId, UserId.New());
        var d2 = Delegation.Create(DelegationId.New(), delegatorId, UserId.New());
        var other = Delegation.Create(DelegationId.New(), UserId.New(), delegatorId);

        await using (var ctx = CreateContext(dbName))
        {
            var repo = new DelegationRepository(ctx);
            await repo.AddAsync(d1);
            await repo.AddAsync(d2);
            await repo.AddAsync(other);
        }

        await using var readCtx = CreateContext(dbName);
        var result = await new DelegationRepository(readCtx).GetByDelegatorAsync(delegatorId);

        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Equal(delegatorId, d.DelegatorId));
    }

    [Fact]
    public async Task GetByDelegateeAsync_ReturnsOnlyDelegateeDelegations()
    {
        var dbName = nameof(GetByDelegateeAsync_ReturnsOnlyDelegateeDelegations);
        var delegateeId = UserId.New();
        var d1 = Delegation.Create(DelegationId.New(), UserId.New(), delegateeId);
        var d2 = Delegation.Create(DelegationId.New(), UserId.New(), delegateeId);
        var other = Delegation.Create(DelegationId.New(), delegateeId, UserId.New());

        await using (var ctx = CreateContext(dbName))
        {
            var repo = new DelegationRepository(ctx);
            await repo.AddAsync(d1);
            await repo.AddAsync(d2);
            await repo.AddAsync(other);
        }

        await using var readCtx = CreateContext(dbName);
        var result = await new DelegationRepository(readCtx).GetByDelegateeAsync(delegateeId);

        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Equal(delegateeId, d.DelegateeId));
    }
}
