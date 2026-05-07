using Microsoft.Extensions.Options;
using OpenFga.Sdk.Client;

namespace CardTrader.Infrastructure.OpenFga;

/// <summary>
/// Singleton factory that owns the configured OpenFgaClient.
/// Consumers call CreateClient() rather than taking OpenFgaClient directly
/// so we can swap the implementation in tests without changing registrations.
/// </summary>
public sealed class OpenFgaClientFactory
{
    private readonly OpenFgaClient _client;

    public OpenFgaClientFactory(IOptions<OpenFgaOptions> options)
    {
        var o = options.Value;
        _client = new OpenFgaClient(new ClientConfiguration
        {
            ApiUrl = o.ApiUrl,
            StoreId = o.StoreId,
            AuthorizationModelId = o.AuthorizationModelId,
        });
    }

    public OpenFgaClient CreateClient() => _client;
}
