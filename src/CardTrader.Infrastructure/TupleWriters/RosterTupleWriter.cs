using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Infrastructure.OpenFga;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.TupleWriters;

internal sealed class RosterTupleWriter(IFgaWriteClient fga) : ITupleWriterHandler
{
    public bool CanHandle(IDomainEvent @event) => @event is
        RosterCreated or
        RosterSharedWithUser or
        RosterUnshared or
        RosterMadePublic or
        RosterMadePrivate;

    public Task HandleAsync(IDomainEvent @event, CancellationToken ct = default) => @event switch
    {
        RosterCreated e        => HandleAsync(e, ct),
        RosterSharedWithUser e => HandleAsync(e, ct),
        RosterUnshared e       => HandleAsync(e, ct),
        RosterMadePublic e     => HandleAsync(e, ct),
        RosterMadePrivate e    => HandleAsync(e, ct),
        _ => Task.CompletedTask,
    };

    private Task HandleAsync(RosterCreated e, CancellationToken ct) =>
        fga.WriteAsync([T($"user:{e.OwnerId}", FgaRelations.Owner,
            $"{FgaTypes.Roster}:{e.Id}")], ct);

    private Task HandleAsync(RosterSharedWithUser e, CancellationToken ct) =>
        fga.WriteAsync([T($"user:{e.SharedWithUserId}", FgaRelations.Viewer,
            $"{FgaTypes.Roster}:{e.Id}")], ct);

    private Task HandleAsync(RosterUnshared e, CancellationToken ct) =>
        fga.DeleteAsync([D($"user:{e.UnsharedFromUserId}", FgaRelations.Viewer,
            $"{FgaTypes.Roster}:{e.Id}")], ct);

    private Task HandleAsync(RosterMadePublic e, CancellationToken ct) =>
        fga.WriteAsync([T("user:*", FgaRelations.Viewer,
            $"{FgaTypes.Roster}:{e.Id}")], ct);

    private Task HandleAsync(RosterMadePrivate e, CancellationToken ct) =>
        fga.DeleteAsync([D("user:*", FgaRelations.Viewer,
            $"{FgaTypes.Roster}:{e.Id}")], ct);

    private static ClientTupleKey T(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };

    private static ClientTupleKeyWithoutCondition D(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };
}
