using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Infrastructure.OpenFga;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.TupleWriters;

internal sealed class CollectionTupleWriter(IFgaWriteClient fga) : ITupleWriterHandler
{
    public bool CanHandle(IDomainEvent @event) => @event is
        CollectionCreated or
        CollectionSharedWithUser or
        CollectionUnshared or
        CollectionMadePublic or
        CollectionMadePrivate;

    public Task HandleAsync(IDomainEvent @event, CancellationToken ct = default) => @event switch
    {
        CollectionCreated e        => HandleAsync(e, ct),
        CollectionSharedWithUser e => HandleAsync(e, ct),
        CollectionUnshared e       => HandleAsync(e, ct),
        CollectionMadePublic e     => HandleAsync(e, ct),
        CollectionMadePrivate e    => HandleAsync(e, ct),
        _ => Task.CompletedTask,
    };

    private Task HandleAsync(CollectionCreated e, CancellationToken ct) =>
        fga.WriteAsync([T($"user:{e.OwnerId}", FgaRelations.Owner,
            $"{FgaTypes.Collection}:{e.Id}")], ct);

    private Task HandleAsync(CollectionSharedWithUser e, CancellationToken ct) =>
        fga.WriteAsync([T($"user:{e.SharedWithUserId}", FgaRelations.Viewer,
            $"{FgaTypes.Collection}:{e.Id}")], ct);

    private Task HandleAsync(CollectionUnshared e, CancellationToken ct) =>
        fga.DeleteAsync([D($"user:{e.UnsharedFromUserId}", FgaRelations.Viewer,
            $"{FgaTypes.Collection}:{e.Id}")], ct);

    private Task HandleAsync(CollectionMadePublic e, CancellationToken ct) =>
        fga.WriteAsync([T("user:*", FgaRelations.Viewer,
            $"{FgaTypes.Collection}:{e.Id}")], ct);

    private Task HandleAsync(CollectionMadePrivate e, CancellationToken ct) =>
        fga.DeleteAsync([D("user:*", FgaRelations.Viewer,
            $"{FgaTypes.Collection}:{e.Id}")], ct);

    private static ClientTupleKey T(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };

    private static ClientTupleKeyWithoutCondition D(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };
}
