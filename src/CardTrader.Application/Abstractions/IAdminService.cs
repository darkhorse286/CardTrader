using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Abstractions;

/// <summary>
/// Coarse-grained role check. Kept separate from IAuthorizationService because the
/// admin role is RBAC (Identity), not a relationship in the FGA graph, and the
/// implementations have different DI lifetimes (Scoped vs Singleton).
/// </summary>
public interface IAdminService
{
    Task<bool> IsAdminAsync(UserId userId, CancellationToken ct = default);
}
