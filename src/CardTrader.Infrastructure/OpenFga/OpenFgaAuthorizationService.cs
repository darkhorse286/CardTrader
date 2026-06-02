using CardTrader.Application.Abstractions;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.OpenFga;

internal sealed class OpenFgaAuthorizationService(OpenFgaClientFactory factory) : IAuthorizationService
{
    public async Task<bool> CheckAsync(
        string user, string relation, string @object, CancellationToken cancellationToken = default)
    {
        var client = factory.CreateClient();
        var response = await client.Check(new ClientCheckRequest
        {
            User = user,
            Relation = relation,
            Object = @object,
        });
        return response.Allowed == true;
    }

    public async Task<IReadOnlyList<string>> ListObjectsAsync(
        string user, string relation, string type, CancellationToken cancellationToken = default)
    {
        var client = factory.CreateClient();
        var response = await client.ListObjects(new ClientListObjectsRequest
        {
            User = user,
            Relation = relation,
            Type = type,
        }, cancellationToken: cancellationToken);
        return response.Objects ?? [];
    }
}
