using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Persistence.Repositories;

internal sealed class PackClaimRepository(AppDbContext db) : IPackClaimRepository
{
    public async Task<DateTimeOffset?> GetLastClaimAsync(UserId userId, CancellationToken ct = default)
        => (await db.PackClaims.FirstOrDefaultAsync(p => p.UserId == userId, ct))?.LastClaimedAt;

    public async Task SetLastClaimAsync(UserId userId, DateTimeOffset claimedAt, CancellationToken ct = default)
    {
        var existing = await db.PackClaims.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (existing is null)
            db.PackClaims.Add(new PackClaim { UserId = userId, LastClaimedAt = claimedAt });
        else
            existing.LastClaimedAt = claimedAt;
        await db.SaveChangesAsync(ct);
    }
}
