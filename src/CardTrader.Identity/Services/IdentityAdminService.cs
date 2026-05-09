using CardTrader.Application.Abstractions;
using CardTrader.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace CardTrader.Identity.Services;

internal sealed class IdentityAdminService(UserManager<CardTraderUser> userManager) : IAdminService
{
    internal const string AdminRole = "Admin";

    public async Task<bool> IsAdminAsync(UserId userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        return user is not null && await userManager.IsInRoleAsync(user, AdminRole);
    }
}
