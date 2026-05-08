using CardTrader.Application.Abstractions;
using CardTrader.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace CardTrader.Identity.Services;

internal sealed class AspNetCoreUserLookup(UserManager<CardTraderUser> userManager) : IUserLookup
{
    public async Task<UserId?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : new UserId(Guid.Parse(user.Id));
    }
}
