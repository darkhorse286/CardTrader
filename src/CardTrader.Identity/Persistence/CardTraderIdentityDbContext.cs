using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Identity.Persistence;

public sealed class CardTraderIdentityDbContext(DbContextOptions<CardTraderIdentityDbContext> options)
    : IdentityDbContext<CardTraderUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Keep Identity tables in their own schema so they don't clutter the app tables.
        builder.HasDefaultSchema("identity");
    }
}
