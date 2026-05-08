using System.Security.Claims;
using CardTrader.Application.Abstractions;
using CardTrader.Domain.ValueObjects;
using Microsoft.AspNetCore.Components.Authorization;

namespace CardTrader.Identity.Services;

internal sealed class AspNetCoreCurrentUser(AuthenticationStateProvider authStateProvider) : ICurrentUser
{
    public async Task<UserId> GetUserIdAsync(CancellationToken ct = default)
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        var nameId = state.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        return new UserId(Guid.Parse(nameId));
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true;
    }
}
