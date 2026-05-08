using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Infrastructure.OpenFga;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.TupleWriters;

// TradeProposalAccepted and TradeProposalCancelled have no tuple side-effects —
// the FGA tuples serve as read-only audit state once a proposal concludes.
internal sealed class TradeProposalTupleWriter(IFgaWriteClient fga) : ITupleWriterHandler
{
    public bool CanHandle(IDomainEvent @event) => @event is
        TradeProposalCreated or
        TradeProposalFacilitatorAssigned;

    public Task HandleAsync(IDomainEvent @event, CancellationToken ct = default) => @event switch
    {
        TradeProposalCreated e             => HandleAsync(e, ct),
        TradeProposalFacilitatorAssigned e => HandleAsync(e, ct),
        _ => Task.CompletedTask,
    };

    private Task HandleAsync(TradeProposalCreated e, CancellationToken ct) =>
        fga.WriteAsync([
            T($"user:{e.InitiatorId}", FgaRelations.Initiator, $"{FgaTypes.TradeProposal}:{e.Id}"),
            T($"user:{e.RecipientId}", FgaRelations.Recipient, $"{FgaTypes.TradeProposal}:{e.Id}"),
        ], ct);

    private Task HandleAsync(TradeProposalFacilitatorAssigned e, CancellationToken ct) =>
        fga.WriteAsync([T($"user:{e.FacilitatorId}", FgaRelations.Facilitator,
            $"{FgaTypes.TradeProposal}:{e.Id}")], ct);

    private static ClientTupleKey T(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };
}
