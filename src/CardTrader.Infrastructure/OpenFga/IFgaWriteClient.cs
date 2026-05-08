using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.OpenFga;

internal interface IFgaWriteClient
{
    Task WriteAsync(IReadOnlyList<ClientTupleKey> tuples, CancellationToken ct = default);
    Task DeleteAsync(IReadOnlyList<ClientTupleKeyWithoutCondition> tuples, CancellationToken ct = default);
}
