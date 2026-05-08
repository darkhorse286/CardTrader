using CardTrader.Application.Abstractions;
using CardTrader.Identity.Persistence;
using CardTrader.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Identity;

public static class IdentityServiceExtensions
{
    /// <summary>
    /// Registers Identity core services and ICurrentUser. Returns IdentityBuilder so the
    /// calling project can chain UI-specific additions (e.g. .AddDefaultUI() for the web app,
    /// or nothing for an Okta-backed deployment).
    /// </summary>
    public static IdentityBuilder AddCardTraderIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CardTraderIdentityDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));

        services.AddScoped<ICurrentUser, AspNetCoreCurrentUser>();

        // Relaxed password rules for dev/PoC — tighten before any real deployment.
        return services
            .AddIdentity<CardTraderUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<CardTraderIdentityDbContext>()
            .AddDefaultTokenProviders();
    }
}
