using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Persistence.Repositories;

internal sealed class CardRepository(AppDbContext db) : ICardRepository
{
    public Task<Card?> GetByIdAsync(CardId id, CancellationToken ct = default)
        => db.Cards.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Card card, CancellationToken ct = default)
    {
        await db.Cards.AddAsync(card, ct);
        await db.SaveChangesAsync(ct);
    }
}
