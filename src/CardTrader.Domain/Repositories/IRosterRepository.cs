using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Repositories;

public interface IRosterRepository
{
    Task<Roster?> GetByIdAsync(RosterId id, CancellationToken ct = default);
    Task<IReadOnlyList<Roster>> GetAllByOwnerAsync(UserId ownerId, CancellationToken ct = default);
    Task AddAsync(Roster roster, CancellationToken ct = default);
}
