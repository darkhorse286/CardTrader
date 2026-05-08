using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Authorization.Tests;

[CollectionDefinition(Name)]
public sealed class OpenFgaCollection : ICollectionFixture<OpenFgaFixture>
{
    public const string Name = "OpenFga";
}

/// <summary>
/// Starts a single OpenFGA container (in-memory datastore) for the test run.
/// Each test class gets its own store + model write so tuple state is isolated.
/// </summary>
public sealed class OpenFgaFixture : IAsyncLifetime
{
    private IContainer? _container;

    public string ApiUrl { get; private set; } = "";

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("openfga/openfga:latest")
            .WithCommand("run")
            .WithEnvironment("OPENFGA_DATASTORE_ENGINE", "memory")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r
                    .ForPort((ushort)8080)
                    .ForPath("/healthz")))
            .Build();

        await _container.StartAsync();

        var port = _container.GetMappedPublicPort(8080);
        ApiUrl = $"http://localhost:{port}";
    }

    /// <summary>
    /// Creates a fresh store with the current authorization model.
    /// Call once per test class in its own IAsyncLifetime.InitializeAsync.
    /// </summary>
    public async Task<OpenFgaClient> CreateStoreWithModelAsync(string storeName)
    {
        var bootstrap = new OpenFgaClient(new ClientConfiguration { ApiUrl = ApiUrl });
        var store = await bootstrap.CreateStore(new ClientCreateStoreRequest { Name = storeName });

        var storeClient = new OpenFgaClient(new ClientConfiguration
        {
            ApiUrl = ApiUrl,
            StoreId = store.Id,
        });

        var model = await storeClient.WriteAuthorizationModel(AuthorizationModelBuilder.Build());

        return new OpenFgaClient(new ClientConfiguration
        {
            ApiUrl = ApiUrl,
            StoreId = store.Id,
            AuthorizationModelId = model.AuthorizationModelId,
        });
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
