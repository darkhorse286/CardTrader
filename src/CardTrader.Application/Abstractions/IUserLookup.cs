using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Abstractions;

/// <summary>
/// Resolves user identities by email address or user ID.
/// Implementations live in CardTrader.Identity — swap for Okta or another provider
/// without touching the Application layer.
/// </summary>
public interface IUserLookup
{
    /// <summary>Returns the UserId for the given email, or null if no account exists.</summary>
    Task<UserId?> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Returns the email address for the given UserId, or null if no account exists.</summary>
    Task<string?> GetEmailByIdAsync(UserId userId, CancellationToken ct = default);
}
