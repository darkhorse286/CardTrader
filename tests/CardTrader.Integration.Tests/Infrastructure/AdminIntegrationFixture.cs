using CardTrader.Application;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using CardTrader.Identity;
using CardTrader.Identity.Persistence;
using CardTrader.Infrastructure;
using CardTrader.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using Testcontainers.PostgreSql;

namespace CardTrader.Integration.Tests.Infrastructure;

public sealed class AdminIntegrationFixture : IAsyncLifetime
{
    private IContainer? _fgaContainer;
    private PostgreSqlContainer? _postgresContainer;
    private IServiceProvider? _serviceProvider;

    public UserId AdminUserId { get; private set; }

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine").Build();

        _fgaContainer = new ContainerBuilder("openfga/openfga:v1.15.1")
            .WithCommand("run")
            .WithEnvironment("OPENFGA_DATASTORE_ENGINE", "memory")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r
                    .ForPort((ushort)8080)
                    .ForPath("/healthz")))
            .Build();

        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _fgaContainer.StartAsync());

        var fgaApiUrl = $"http://localhost:{_fgaContainer.GetMappedPublicPort(8080)}";
        var connectionString = _postgresContainer.GetConnectionString();

        var bootstrap = new OpenFgaClient(new ClientConfiguration { ApiUrl = fgaApiUrl });
        var store = await bootstrap.CreateStore(new ClientCreateStoreRequest { Name = "integration-admin" });
        var storeClient = new OpenFgaClient(new ClientConfiguration
        {
            ApiUrl = fgaApiUrl,
            StoreId = store.Id,
        });
        var model = await storeClient.WriteAuthorizationModel(AuthorizationModelBuilder.Build());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenFga:ApiUrl"] = fgaApiUrl,
                ["OpenFga:StoreId"] = store.Id,
                ["OpenFga:AuthorizationModelId"] = model.AuthorizationModelId,
                ["ConnectionStrings:DefaultConnection"] = connectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDataProtection();
        services.AddCardTraderIdentity(configuration);
        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        var identityDb = scope.ServiceProvider.GetRequiredService<CardTraderIdentityDbContext>();
        await identityDb.Database.MigrateAsync();

        AdminUserId = UserId.New();
        await SeedAdminAsync(scope.ServiceProvider, AdminUserId);
    }

    private static async Task SeedAdminAsync(IServiceProvider sp, UserId adminId)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<CardTraderUser>>();

        if (!await roleManager.RoleExistsAsync(CardTraderRoles.Admin))
            await roleManager.CreateAsync(new IdentityRole(CardTraderRoles.Admin));

        var user = new CardTraderUser
        {
            Id = adminId.Value.ToString(),
            UserName = "admin@integration.test",
            Email = "admin@integration.test",
        };
        await userManager.CreateAsync(user, "Admin123!");
        await userManager.AddToRoleAsync(user, CardTraderRoles.Admin);
    }

    public IServiceScope CreateScope() => _serviceProvider!.CreateScope();

    public async Task DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable d)
            await d.DisposeAsync();
        if (_fgaContainer is not null)
            await _fgaContainer.DisposeAsync();
        if (_postgresContainer is not null)
            await _postgresContainer.DisposeAsync();
    }
}
