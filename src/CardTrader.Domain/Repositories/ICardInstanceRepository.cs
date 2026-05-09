using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Repositories;

public interface ICardInstanceRepository
{
    Task<CardInstance?> GetByIdAsync(CardInstanceId id, CancellationToken ct = default);
    Task<IReadOnlyList<CardInstance>> GetByCollectionAsync(CollectionId collectionId, CancellationToken ct = default);
    Task AddAsync(CardInstance instance, CancellationToken ct = default);
    Task UpdateAsync(CardInstance instance, CancellationToken ct = default);
}
