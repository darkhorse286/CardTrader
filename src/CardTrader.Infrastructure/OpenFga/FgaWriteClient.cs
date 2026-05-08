using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.OpenFga;

internal sealed class FgaWriteClient(OpenFgaClientFactory factory) : IFgaWriteClient
{
    public async Task WriteAsync(IReadOnlyList<ClientTupleKey> tuples, CancellationToken ct = default)
    {
        if (tuples.Count == 0) return;
        await factory.CreateClient().Write(new ClientWriteRequest { Writes = [..tuples] });
    }

    public async Task DeleteAsync(IReadOnlyList<ClientTupleKeyWithoutCondition> tuples, CancellationToken ct = default)
    {
        if (tuples.Count == 0) return;
        await factory.CreateClient().Write(new ClientWriteRequest { Deletes = [..tuples] });
    }
}
