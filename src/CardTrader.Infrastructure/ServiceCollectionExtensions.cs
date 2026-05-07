using CardTrader.Infrastructure.OpenFga;
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

        return services;
    }
}
