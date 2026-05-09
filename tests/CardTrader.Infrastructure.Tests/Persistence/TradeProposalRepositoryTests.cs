using CardTrader.Domain.Entities;
using CardTrader.Domain.Enums;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.Persistence;
using CardTrader.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Tests.Persistence;

public class TradeProposalRepositoryTests
{
    private static AppDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    private static (TradeProposalId, UserId, UserId) MakeIds() =>
        (TradeProposalId.New(), UserId.New(), UserId.New());

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var ctx = CreateContext(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
        var repo = new TradeProposalRepository(ctx);

        var result = await repo.GetByIdAsync(TradeProposalId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsProposal()
    {
        var dbName = nameof(AddAsync_PersistsProposal);
        var (id, initiator, recipient) = MakeIds();
        var proposal = TradeProposal.Create(id, initiator, recipient);

        await using (var writeCtx = CreateContext(dbName))
            await new TradeProposalRepository(writeCtx).AddAsync(proposal);

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.TradeProposals.FirstOrDefaultAsync(p => p.Id == id);
        Assert.NotNull(stored);
        Assert.Equal(initiator, stored.InitiatorId);
        Assert.Equal(recipient, stored.RecipientId);
        Assert.Equal(TradeProposalStatus.Pending, stored.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProposal_WhenFound()
    {
        var dbName = nameof(GetByIdAsync_ReturnsProposal_WhenFound);
        var (id, initiator, recipient) = MakeIds();
        var proposal = TradeProposal.Create(id, initiator, recipient);

        await using (var writeCtx = CreateContext(dbName))
            await new TradeProposalRepository(writeCtx).AddAsync(proposal);

        await using var readCtx = CreateContext(dbName);
        var result = await new TradeProposalRepository(readCtx).GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetInvolvingUserAsync_ReturnsOnlyInvolvingProposals()
    {
        var dbName = nameof(GetInvolvingUserAsync_ReturnsOnlyInvolvingProposals);
        var target = UserId.New();
        var other1 = UserId.New();
        var other2 = UserId.New();

        await using (var ctx = CreateContext(dbName))
        {
            var repo = new TradeProposalRepository(ctx);
            // target is initiator
            await repo.AddAsync(TradeProposal.Create(TradeProposalId.New(), target, other1));
            // target is recipient
            await repo.AddAsync(TradeProposal.Create(TradeProposalId.New(), other1, target));
            // target not involved
            await repo.AddAsync(TradeProposal.Create(TradeProposalId.New(), other1, other2));
        }

        await using var readCtx = CreateContext(dbName);
        var results = await new TradeProposalRepository(readCtx).GetInvolvingUserAsync(target);

        Assert.Equal(2, results.Count);
        Assert.All(results, p => Assert.True(p.InitiatorId == target || p.RecipientId == target));
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        var dbName = nameof(UpdateAsync_PersistsStatusChange);
        var (id, initiator, recipient) = MakeIds();

        await using (var writeCtx = CreateContext(dbName))
        {
            var proposal = TradeProposal.Create(id, initiator, recipient);
            await new TradeProposalRepository(writeCtx).AddAsync(proposal);
        }

        await using (var updateCtx = CreateContext(dbName))
        {
            var proposal = await new TradeProposalRepository(updateCtx).GetByIdAsync(id);
            proposal!.Accept();
            await new TradeProposalRepository(updateCtx).UpdateAsync(proposal);
        }

        await using var readCtx = CreateContext(dbName);
        var stored = await readCtx.TradeProposals.FirstOrDefaultAsync(p => p.Id == id);
        Assert.NotNull(stored);
        Assert.Equal(TradeProposalStatus.Accepted, stored.Status);
    }

    [Fact]
    public async Task GetAllPendingAsync_ReturnsOnlyPendingProposals()
    {
        var dbName = nameof(GetAllPendingAsync_ReturnsOnlyPendingProposals);

        await using (var ctx = CreateContext(dbName))
        {
            var repo = new TradeProposalRepository(ctx);
            var (id1, i1, r1) = MakeIds();
            var (id2, i2, r2) = MakeIds();
            var (id3, i3, r3) = MakeIds();

            var pending = TradeProposal.Create(id1, i1, r1);
            var accepted = TradeProposal.Create(id2, i2, r2);
            var cancelled = TradeProposal.Create(id3, i3, r3);

            await repo.AddAsync(pending);
            await repo.AddAsync(accepted);
            await repo.AddAsync(cancelled);

            accepted.Accept();
            cancelled.Cancel();
            await repo.UpdateAsync(accepted);
            await repo.UpdateAsync(cancelled);
        }

        await using var readCtx = CreateContext(dbName);
        var result = await new TradeProposalRepository(readCtx).GetAllPendingAsync();

        Assert.Single(result);
        Assert.Equal(TradeProposalStatus.Pending, result[0].Status);
    }
}
