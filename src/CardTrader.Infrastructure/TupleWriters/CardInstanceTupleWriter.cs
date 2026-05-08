using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Infrastructure.OpenFga;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.TupleWriters;

internal sealed class CardInstanceTupleWriter(IFgaWriteClient fga) : ITupleWriterHandler
{
    public bool CanHandle(IDomainEvent @event) => @event is
        CardInstanceCreated or
        CardInstanceAddedToCollection or
        CardInstanceRemovedFromCollection;

    public Task HandleAsync(IDomainEvent @event, CancellationToken ct = default) => @event switch
    {
        CardInstanceCreated e            => HandleAsync(e, ct),
        CardInstanceAddedToCollection e  => HandleAsync(e, ct),
        CardInstanceRemovedFromCollection e => HandleAsync(e, ct),
        _ => Task.CompletedTask,
    };

    private Task HandleAsync(CardInstanceCreated e, CancellationToken ct) =>
        fga.WriteAsync([T($"user:{e.OwnerId}", FgaRelations.Owner,
            $"{FgaTypes.CardInstance}:{e.Id}")], ct);

    private Task HandleAsync(CardInstanceAddedToCollection e, CancellationToken ct) =>
        fga.WriteAsync([T($"{FgaTypes.Collection}:{e.CollectionId}", FgaRelations.Collection,
            $"{FgaTypes.CardInstance}:{e.Id}")], ct);

    private Task HandleAsync(CardInstanceRemovedFromCollection e, CancellationToken ct) =>
        fga.DeleteAsync([D($"{FgaTypes.Collection}:{e.CollectionId}", FgaRelations.Collection,
            $"{FgaTypes.CardInstance}:{e.Id}")], ct);

    private static ClientTupleKey T(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };

    private static ClientTupleKeyWithoutCondition D(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };
}
