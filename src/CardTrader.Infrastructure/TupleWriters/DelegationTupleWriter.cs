using CardTrader.Authorization.Conditions;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Infrastructure.OpenFga;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;

namespace CardTrader.Infrastructure.TupleWriters;

internal sealed class DelegationTupleWriter(IFgaWriteClient fga) : ITupleWriterHandler
{
    public bool CanHandle(IDomainEvent @event) => @event is
        DelegationCreated or
        DelegationActivated or
        DelegationRevoked;

    public Task HandleAsync(IDomainEvent @event, CancellationToken ct = default) => @event switch
    {
        DelegationCreated e   => HandleAsync(e, ct),
        DelegationActivated e => HandleAsync(e, ct),
        DelegationRevoked e   => HandleAsync(e, ct),
        _ => Task.CompletedTask,
    };

    private Task HandleAsync(DelegationCreated e, CancellationToken ct) =>
        fga.WriteAsync([
            T($"user:{e.DelegatorId}", FgaRelations.Delegator, $"{FgaTypes.Delegation}:{e.Id}"),
            T($"user:{e.DelegateeId}", FgaRelations.Delegatee, $"{FgaTypes.Delegation}:{e.Id}"),
        ], ct);

    private Task HandleAsync(DelegationActivated e, CancellationToken ct)
    {
        var obj = $"{FgaTypes.Delegation}:{e.Id}";
        var user = $"user:{e.DelegatorId}";

        ClientTupleKey tuple = e.ExpiresAt is { } expiresAt
            ? new()
            {
                User = user, Relation = FgaRelations.ActiveDelegator, Object = obj,
                Condition = new RelationshipCondition
                {
                    Name = FgaConditions.NotExpired,
                    Context = new { expires_at = expiresAt.ToUniversalTime().ToString("O") },
                },
            }
            : T(user, FgaRelations.ActiveDelegator, obj);

        return fga.WriteAsync([tuple], ct);
    }

    private Task HandleAsync(DelegationRevoked e, CancellationToken ct) =>
        fga.DeleteAsync([D($"user:{e.DelegatorId}", FgaRelations.ActiveDelegator,
            $"{FgaTypes.Delegation}:{e.Id}")], ct);

    private static ClientTupleKey T(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };

    private static ClientTupleKeyWithoutCondition D(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };
}
