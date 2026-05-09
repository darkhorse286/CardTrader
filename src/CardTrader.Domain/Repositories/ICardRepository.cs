using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Repositories;

public interface ICardRepository
{
    Task<Card?> GetByIdAsync(CardId id, CancellationToken ct = default);
    Task<IReadOnlyList<Card>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Card card, CancellationToken ct = default);
}
