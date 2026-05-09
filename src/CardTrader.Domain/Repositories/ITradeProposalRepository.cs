using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Repositories;

public interface ITradeProposalRepository
{
    Task<TradeProposal?> GetByIdAsync(TradeProposalId id, CancellationToken ct = default);
    Task<IReadOnlyList<TradeProposal>> GetInvolvingUserAsync(UserId userId, CancellationToken ct = default);
    Task<IReadOnlyList<TradeProposal>> GetAllPendingAsync(CancellationToken ct = default);
    Task AddAsync(TradeProposal proposal, CancellationToken ct = default);
    Task UpdateAsync(TradeProposal proposal, CancellationToken ct = default);
}
