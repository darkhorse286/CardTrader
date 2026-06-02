using CardTrader.Application.Abstractions;
using CardTrader.Domain.Repositories;
using CardTrader.Infrastructure.OpenFga;
using CardTrader.Infrastructure.Persistence;
using CardTrader.Infrastructure.Persistence.Repositories;
using CardTrader.Infrastructure.TupleWriters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // OpenFGA
        services.Configure<OpenFgaOptions>(
            configuration.GetSection(OpenFgaOptions.SectionName));
        services.AddSingleton<OpenFgaClientFactory>();
        services.AddSingleton<IAuthorizationService, OpenFgaAuthorizationService>();
        services.AddSingleton<IFgaWriteClient, FgaWriteClient>();

        // Tuple writers
        services.AddSingleton<ITupleWriterHandler, CardInstanceTupleWriter>();
        services.AddSingleton<ITupleWriterHandler, RosterTupleWriter>();
        services.AddSingleton<ITupleWriterHandler, TradeProposalTupleWriter>();
        services.AddSingleton<ITupleWriterHandler, DelegationTupleWriter>();
        services.AddSingleton<IDomainEventDispatcher, TupleWriterDispatcher>();

        // Persistence
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<ICardInstanceRepository, CardInstanceRepository>();
        services.AddScoped<IRosterRepository, RosterRepository>();
        services.AddScoped<ITradeProposalRepository, TradeProposalRepository>();
        services.AddScoped<IDelegationRepository, DelegationRepository>();
        services.AddScoped<IPackClaimRepository, PackClaimRepository>();

        return services;
    }
}
