using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CardTrader.Infrastructure.Persistence;

// Used by `dotnet ef migrations add` — does not run through the Web startup project.
// Set CARDTRADER_DB_CONNECTION before running migrations:
//   $env:CARDTRADER_DB_CONNECTION = "Host=localhost;Port=5432;Database=cardtrader;Username=...;Password=..."
internal sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CARDTRADER_DB_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set the CARDTRADER_DB_CONNECTION environment variable before running migrations.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AppDbContext(options);
    }
}
