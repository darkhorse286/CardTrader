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
            // current_time satisfies the not_expired condition on time-bound tuples.
            // Extra context is harmless on tuples with no condition.
            Context = new Dictionary<string, object>
            {
                ["current_time"] = DateTimeOffset.UtcNow.ToString("O"),
            },
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
            // current_time satisfies the not_expired condition on time-bound tuples.
            // Matches CheckAsync so both paths apply conditions consistently.
            Context = new Dictionary<string, object>
            {
                ["current_time"] = DateTimeOffset.UtcNow.ToString("O"),
            },
        }, cancellationToken: cancellationToken);
        return response.Objects ?? [];
    }
}
