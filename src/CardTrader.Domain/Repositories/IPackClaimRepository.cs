using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Repositories;

public interface IPackClaimRepository
{
    Task<DateTimeOffset?> GetLastClaimAsync(UserId userId, CancellationToken ct = default);
    Task SetLastClaimAsync(UserId userId, DateTimeOffset claimedAt, CancellationToken ct = default);
}
