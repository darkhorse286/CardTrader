using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Repositories;

public interface ICollectionRepository
{
    Task<Collection?> GetByIdAsync(CollectionId id, CancellationToken ct = default);
    Task AddAsync(Collection collection, CancellationToken ct = default);
}
