using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;

namespace CardTrader.Application.Services;

public sealed class CardService(ICardRepository cards)
{
    public Task<IReadOnlyList<Card>> GetAllAsync(CancellationToken ct = default)
        => cards.GetAllAsync(ct);
}
