using CardTrader.Application.Abstractions;
using CardTrader.Infrastructure.OpenFga;
using CardTrader.Infrastructure.TupleWriters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpenFgaOptions>(
            configuration.GetSection(OpenFgaOptions.SectionName));

        services.AddSingleton<OpenFgaClientFactory>();
        services.AddSingleton<IAuthorizationService, OpenFgaAuthorizationService>();
        services.AddSingleton<IFgaWriteClient, FgaWriteClient>();

        services.AddSingleton<ITupleWriterHandler, CardInstanceTupleWriter>();
        services.AddSingleton<ITupleWriterHandler, CollectionTupleWriter>();
        services.AddSingleton<ITupleWriterHandler, TradeProposalTupleWriter>();
        services.AddSingleton<ITupleWriterHandler, DelegationTupleWriter>();
        services.AddSingleton<IDomainEventDispatcher, TupleWriterDispatcher>();

        return services;
    }
}
