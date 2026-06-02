using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Domain.Repositories;
using CardTrader.Infrastructure.OpenFga;
using Microsoft.Extensions.DependencyInjection;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.TupleWriters;

// TradeProposalAccepted and TradeProposalCancelled have no tuple side-effects —
// the FGA tuples serve as read-only audit state once a proposal concludes.
internal sealed class TradeProposalTupleWriter(
    IFgaWriteClient fga,
    IServiceScopeFactory scopeFactory) : ITupleWriterHandler
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

    private async Task HandleAsync(TradeProposalCreated e, CancellationToken ct)
    {
        await fga.WriteAsync([
            T($"{FgaTypes.User}:{e.InitiatorId}", FgaRelations.Initiator, $"{FgaTypes.TradeProposal}:{e.Id}"),
            T($"{FgaTypes.User}:{e.RecipientId}", FgaRelations.Recipient, $"{FgaTypes.TradeProposal}:{e.Id}"),
        ], ct);

        // Write supervisor tuples for any delegators who are currently active over the
        // initiator or recipient, so that parent-managed accounts can oversee the proposal.
        await using var scope = scopeFactory.CreateAsyncScope();
        var delegRepo = scope.ServiceProvider.GetRequiredService<IDelegationRepository>();

        var supervisorTuples = new List<ClientTupleKey>();
        foreach (var participantId in new[] { e.InitiatorId, e.RecipientId })
        {
            var delegations = await delegRepo.GetByDelegateeAsync(participantId, ct);
            foreach (var d in delegations.Where(d => d.IsActive))
                supervisorTuples.Add(
                    T($"{FgaTypes.Delegation}:{d.Id}#active_delegator",
                      FgaRelations.Supervisor,
                      $"{FgaTypes.TradeProposal}:{e.Id}"));
        }

        if (supervisorTuples.Count > 0)
            await fga.WriteAsync(supervisorTuples, ct);
    }

    private Task HandleAsync(TradeProposalFacilitatorAssigned e, CancellationToken ct) =>
        fga.WriteAsync([T($"{FgaTypes.User}:{e.FacilitatorId}", FgaRelations.Facilitator,
            $"{FgaTypes.TradeProposal}:{e.Id}")], ct);

    private static ClientTupleKey T(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };
}
