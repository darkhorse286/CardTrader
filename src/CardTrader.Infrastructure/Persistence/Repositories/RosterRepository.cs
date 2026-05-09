using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Persistence.Repositories;

internal sealed class RosterRepository(AppDbContext db) : IRosterRepository
{
    public Task<Roster?> GetByIdAsync(RosterId id, CancellationToken ct = default)
        => db.Rosters.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Roster>> GetAllByOwnerAsync(UserId ownerId, CancellationToken ct = default)
        => await db.Rosters.Where(c => c.OwnerId == ownerId).ToListAsync(ct);

    public async Task AddAsync(Roster roster, CancellationToken ct = default)
    {
        await db.Rosters.AddAsync(roster, ct);
        await db.SaveChangesAsync(ct);
    }
}
