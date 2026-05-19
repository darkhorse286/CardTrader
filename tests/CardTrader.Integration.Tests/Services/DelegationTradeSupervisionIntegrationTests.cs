using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Enums;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using CardTrader.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Integration.Tests.Services;

/// <summary>
/// Evidence that delegation supervision grants can_cancel on trade proposals
/// where the delegatee is a participant, via the FGA userset path:
///   delegation#active_delegator → supervisor → can_cancel
/// </summary>
[Collection(IntegrationCollection.Name)]
public class DelegationTradeSupervisionIntegrationTests(IntegrationFixture fixture)
{
    private DelegationService DelegSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<DelegationService>();
    private TradeProposalService TradeSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<TradeProposalService>();
    private IAuthorizationService Authz(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    private ITradeProposalRepository TradeRepo(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<ITradeProposalRepository>();

    private Task<bool> CanCancel(IServiceScope scope, UserId user, TradeProposalId trade) =>
        Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{user}",
            FgaRelations.CanCancel,
            $"{FgaTypes.TradeProposal}:{trade}");

    // Activation must happen BEFORE trade creation — the tuple writer queries active
    // delegations at trade-creation time and writes supervisor tuples then.

    [Fact]
    public async Task ActiveDelegator_CanCancel_WhenDelegateeIsInitiator()
    {
        using var scope = fixture.CreateScope();
        var delegator = UserId.New();
        var delegatee = UserId.New();
        var recipient = UserId.New();
        var delegId   = DelegationId.New();
        var tradeId   = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        await DelegSvc(scope).ActivateAsync(delegId, delegator);

        // Trade created after activation — supervisor tuple is written by the tuple writer.
        await TradeSvc(scope).CreateAsync(tradeId, delegatee, recipient);

        Assert.True(await CanCancel(scope, delegator, tradeId));
    }

    [Fact]
    public async Task ActiveDelegator_CanCancel_WhenDelegateeIsRecipient()
    {
        using var scope = fixture.CreateScope();
        var delegator = UserId.New();
        var delegatee = UserId.New();
        var initiator = UserId.New();
        var delegId   = DelegationId.New();
        var tradeId   = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        await DelegSvc(scope).ActivateAsync(delegId, delegator);
        await TradeSvc(scope).CreateAsync(tradeId, initiator, delegatee);

        Assert.True(await CanCancel(scope, delegator, tradeId));
    }

    [Fact]
    public async Task ActiveDelegator_CancelAsync_DoesNotThrow()
    {
        using var scope = fixture.CreateScope();
        var delegator = UserId.New();
        var delegatee = UserId.New();
        var recipient = UserId.New();
        var delegId   = DelegationId.New();
        var tradeId   = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        await DelegSvc(scope).ActivateAsync(delegId, delegator);
        await TradeSvc(scope).CreateAsync(tradeId, delegatee, recipient);

        await TradeSvc(scope).CancelAsync(tradeId, delegator);

        var stored = await TradeRepo(scope).GetByIdAsync(tradeId);
        Assert.NotNull(stored);
        Assert.Equal(TradeProposalStatus.Cancelled, stored.Status);
    }

    [Fact]
    public async Task InactiveDelegation_DelegatorCannotCancel()
    {
        // Delegation exists but was never activated — no supervisor tuple is written.
        using var scope = fixture.CreateScope();
        var delegator = UserId.New();
        var delegatee = UserId.New();
        var recipient = UserId.New();
        var delegId   = DelegationId.New();
        var tradeId   = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        // Intentionally NOT calling ActivateAsync
        await TradeSvc(scope).CreateAsync(tradeId, delegatee, recipient);

        Assert.False(await CanCancel(scope, delegator, tradeId));
    }

    [Fact]
    public async Task AfterRevocation_DelegatorLosesCancelAccess()
    {
        // Revoking the delegation removes the active_delegator tuple; FGA evaluates
        // the delegation#active_delegator userset as empty, revoking supervisor access.
        using var scope = fixture.CreateScope();
        var delegator = UserId.New();
        var delegatee = UserId.New();
        var recipient = UserId.New();
        var delegId   = DelegationId.New();
        var tradeId   = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        await DelegSvc(scope).ActivateAsync(delegId, delegator);
        await TradeSvc(scope).CreateAsync(tradeId, delegatee, recipient);
        Assert.True(await CanCancel(scope, delegator, tradeId)); // confirm access before revoke

        await DelegSvc(scope).RevokeAsync(delegId, delegator);

        Assert.False(await CanCancel(scope, delegator, tradeId));
    }

    [Fact]
    public async Task UnrelatedDelegator_CannotCancel()
    {
        // A delegator whose delegatee is not a trade participant gets no supervisor tuple.
        using var scope = fixture.CreateScope();
        var delegator  = UserId.New();
        var delegatee  = UserId.New();
        var delegId    = DelegationId.New();
        var tradeId    = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        await DelegSvc(scope).ActivateAsync(delegId, delegator);

        // Trade does NOT involve delegatee — delegator should get no supervisor tuple.
        await TradeSvc(scope).CreateAsync(tradeId, UserId.New(), UserId.New());

        Assert.False(await CanCancel(scope, delegator, tradeId));
    }
}
