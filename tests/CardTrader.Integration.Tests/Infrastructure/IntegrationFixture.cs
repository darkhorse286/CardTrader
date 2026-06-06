using CardTrader.Application;
using CardTrader.Application.Abstractions;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure;
using CardTrader.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using Testcontainers.PostgreSql;

namespace CardTrader.Integration.Tests.Infrastructure;

// Admin is not exercised in integration tests — always returns false.
file sealed class NullAdminService : IAdminService
{
    public Task<bool> IsAdminAsync(UserId userId, CancellationToken ct = default) =>
        Task.FromResult(false);
}

public sealed class IntegrationFixture : IAsyncLifetime
{
    private IContainer? _fgaContainer;
    private PostgreSqlContainer? _postgresContainer;
    private IServiceProvider? _serviceProvider;

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

        // Create FGA store and write model
        var bootstrap = new OpenFgaClient(new ClientConfiguration { ApiUrl = fgaApiUrl });
        var store = await bootstrap.CreateStore(new ClientCreateStoreRequest { Name = "integration" });
        var storeClient = new OpenFgaClient(new ClientConfiguration
        {
            ApiUrl = fgaApiUrl,
            StoreId = store.Id,
        });
        var model = await storeClient.WriteAuthorizationModel(AuthorizationModelBuilder.Build());

        // Build full service provider via the same extension methods the app uses
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
        services.AddScoped<IAdminService, NullAdminService>();
        _serviceProvider = services.BuildServiceProvider();

        // Apply EF migrations against the live Postgres container
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
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
