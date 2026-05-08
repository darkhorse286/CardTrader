using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CardTrader.Identity.Persistence;

internal sealed class IdentityDesignTimeFactory : IDesignTimeDbContextFactory<CardTraderIdentityDbContext>
{
    public CardTraderIdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CARDTRADER_DB_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set the CARDTRADER_DB_CONNECTION environment variable before running migrations.");

        var options = new DbContextOptionsBuilder<CardTraderIdentityDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity"))
            .Options;

        return new CardTraderIdentityDbContext(options);
    }
}
