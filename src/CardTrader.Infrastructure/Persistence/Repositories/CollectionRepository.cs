using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Persistence.Repositories;

internal sealed class CollectionRepository(AppDbContext db) : ICollectionRepository
{
    public Task<Collection?> GetByIdAsync(CollectionId id, CancellationToken ct = default)
        => db.Collections.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Collection>> GetAllByOwnerAsync(UserId ownerId, CancellationToken ct = default)
        => await db.Collections.Where(c => c.OwnerId == ownerId).ToListAsync(ct);

    public async Task AddAsync(Collection collection, CancellationToken ct = default)
    {
        await db.Collections.AddAsync(collection, ct);
        await db.SaveChangesAsync(ct);
    }
}
