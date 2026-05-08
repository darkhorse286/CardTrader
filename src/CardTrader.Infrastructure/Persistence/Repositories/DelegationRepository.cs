using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Persistence.Repositories;

internal sealed class DelegationRepository(AppDbContext db) : IDelegationRepository
{
    public Task<Delegation?> GetByIdAsync(DelegationId id, CancellationToken ct = default)
        => db.Delegations.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(Delegation delegation, CancellationToken ct = default)
    {
        await db.Delegations.AddAsync(delegation, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Delegation delegation, CancellationToken ct = default)
    {
        db.Delegations.Update(delegation);
        await db.SaveChangesAsync(ct);
    }
}
