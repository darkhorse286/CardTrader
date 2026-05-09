using CardTrader.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardTrader.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardInstance> CardInstances => Set<CardInstance>();
    public DbSet<Roster> Rosters => Set<Roster>();
    public DbSet<TradeProposal> TradeProposals => Set<TradeProposal>();
    public DbSet<Delegation> Delegations => Set<Delegation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
