using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Repositories;

public interface ICardInstanceRepository
{
    Task<CardInstance?> GetByIdAsync(CardInstanceId id, CancellationToken ct = default);
    Task<IReadOnlyList<CardInstance>> GetByRosterAsync(RosterId rosterId, CancellationToken ct = default);
    Task<IReadOnlyList<CardInstance>> GetByOwnerAsync(UserId ownerId, CancellationToken ct = default);
    Task<bool> ExistsByCardAndPrintNumberAsync(CardId cardId, int printNumber, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetTakenPrintNumbersAsync(CardId cardId, CancellationToken ct = default);
    Task AddAsync(CardInstance instance, CancellationToken ct = default);
    Task UpdateAsync(CardInstance instance, CancellationToken ct = default);
}
