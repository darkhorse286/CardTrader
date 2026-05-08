using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Abstractions;

/// <summary>
/// Resolves the identity of the currently authenticated user.
/// Implementations live in CardTrader.Identity — this assembly never references
/// ASP.NET Core Identity or any specific auth provider directly.
/// </summary>
public interface ICurrentUser
{
    Task<UserId> GetUserIdAsync(CancellationToken ct = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
}
