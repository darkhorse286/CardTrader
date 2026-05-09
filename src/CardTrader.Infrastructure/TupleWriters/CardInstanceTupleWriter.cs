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
        CardInstanceAddedToRoster or
        CardInstanceRemovedFromRoster;

    public Task HandleAsync(IDomainEvent @event, CancellationToken ct = default) => @event switch
    {
        CardInstanceCreated e          => HandleAsync(e, ct),
        CardInstanceAddedToRoster e    => HandleAsync(e, ct),
        CardInstanceRemovedFromRoster e => HandleAsync(e, ct),
        _ => Task.CompletedTask,
    };

    private Task HandleAsync(CardInstanceCreated e, CancellationToken ct) =>
        fga.WriteAsync([T($"user:{e.OwnerId}", FgaRelations.Owner,
            $"{FgaTypes.CardInstance}:{e.Id}")], ct);

    private Task HandleAsync(CardInstanceAddedToRoster e, CancellationToken ct) =>
        fga.WriteAsync([T($"{FgaTypes.Roster}:{e.RosterId}", FgaRelations.Roster,
            $"{FgaTypes.CardInstance}:{e.Id}")], ct);

    private Task HandleAsync(CardInstanceRemovedFromRoster e, CancellationToken ct) =>
        fga.DeleteAsync([D($"{FgaTypes.Roster}:{e.RosterId}", FgaRelations.Roster,
            $"{FgaTypes.CardInstance}:{e.Id}")], ct);

    private static ClientTupleKey T(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };

    private static ClientTupleKeyWithoutCondition D(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };
}
