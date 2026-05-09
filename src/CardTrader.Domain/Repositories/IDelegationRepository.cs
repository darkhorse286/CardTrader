using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Repositories;

public interface IDelegationRepository
{
    Task<Delegation?> GetByIdAsync(DelegationId id, CancellationToken ct = default);
    Task<IReadOnlyList<Delegation>> GetByDelegatorAsync(UserId delegatorId, CancellationToken ct = default);
    Task<IReadOnlyList<Delegation>> GetByDelegateeAsync(UserId delegateeId, CancellationToken ct = default);
    Task AddAsync(Delegation delegation, CancellationToken ct = default);
    Task UpdateAsync(Delegation delegation, CancellationToken ct = default);
}
