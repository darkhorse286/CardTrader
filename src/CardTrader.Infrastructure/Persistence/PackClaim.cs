using CardTrader.Domain.ValueObjects;

namespace CardTrader.Infrastructure.Persistence;

internal sealed class PackClaim
{
    public UserId UserId { get; set; }
    public DateTimeOffset LastClaimedAt { get; set; }
}
