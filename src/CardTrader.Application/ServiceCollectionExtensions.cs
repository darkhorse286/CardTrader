using CardTrader.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CardService>();
        services.AddScoped<CardInstanceService>();
        services.AddScoped<TradeProposalService>();
        services.AddScoped<RosterService>();
        services.AddScoped<DelegationService>();
        return services;
    }
}
