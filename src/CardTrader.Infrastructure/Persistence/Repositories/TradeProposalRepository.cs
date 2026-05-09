using CardTrader.Domain.Entities;
using CardTrader.Domain.Enums;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Persistence.Repositories;

internal sealed class TradeProposalRepository(AppDbContext db) : ITradeProposalRepository
{
    public Task<TradeProposal?> GetByIdAsync(TradeProposalId id, CancellationToken ct = default)
        => db.TradeProposals.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<TradeProposal>> GetInvolvingUserAsync(UserId userId, CancellationToken ct = default)
        => await db.TradeProposals
            .Where(p => p.InitiatorId == userId || p.RecipientId == userId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TradeProposal>> GetAllPendingAsync(CancellationToken ct = default)
        => await db.TradeProposals
            .Where(p => p.Status == TradeProposalStatus.Pending)
            .ToListAsync(ct);

    public async Task AddAsync(TradeProposal proposal, CancellationToken ct = default)
    {
        await db.TradeProposals.AddAsync(proposal, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TradeProposal proposal, CancellationToken ct = default)
    {
        db.TradeProposals.Update(proposal);
        await db.SaveChangesAsync(ct);
    }
}
