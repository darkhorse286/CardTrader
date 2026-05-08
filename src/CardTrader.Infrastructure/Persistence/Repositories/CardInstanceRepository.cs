using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Persistence.Repositories;

internal sealed class CardInstanceRepository(AppDbContext db) : ICardInstanceRepository
{
    public Task<CardInstance?> GetByIdAsync(CardInstanceId id, CancellationToken ct = default)
        => db.CardInstances.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(CardInstance instance, CancellationToken ct = default)
    {
        await db.CardInstances.AddAsync(instance, ct);
        await db.SaveChangesAsync(ct);
    }
}
