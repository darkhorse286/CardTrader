using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Entities;
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
    private CardInstanceService InstanceSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<CardInstanceService>();
    private IAuthorizationService Authz(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    private ITradeProposalRepository TradeRepo(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<ITradeProposalRepository>();

    private Task<bool> CanCancel(IServiceScope scope, UserId user, TradeProposalId trade) =>
        Authz(scope).CheckAsync($"{FgaTypes.User}:{user}", FgaRelations.CanCancel,
            $"{FgaTypes.TradeProposal}:{trade}");

    private async Task<(CardInstanceId, CardInstanceId)> SeedInstancesAsync(
        IServiceScope scope, UserId user1, UserId user2)
    {
        var cardId = CardId.New();
        var cardRepo = scope.ServiceProvider.GetRequiredService<ICardRepository>();
        await cardRepo.AddAsync(Card.Create(cardId, "Trade Card", "Test Set", "Rare", "Player",
            printRun: 500));
        var c1 = CardInstanceId.New();
        var c2 = CardInstanceId.New();
        await InstanceSvc(scope).CreateAsync(c1, cardId, user1, 1);
        await InstanceSvc(scope).CreateAsync(c2, cardId, user2, 2);
        return (c1, c2);
    }

    // Activation must happen BEFORE trade creation — the tuple writer queries active
    // delegations at trade-creation time and writes supervisor tuples then.

    [Fact]
    public async Task ActiveDelegator_CanCancel_WhenDelegateeIsInitiator()
    {
        using var scope = fixture.CreateScope();
        var delegator = UserId.New(); var delegatee = UserId.New(); var recipient = UserId.New();
        var delegId = DelegationId.New(); var tradeId = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        await DelegSvc(scope).ActivateAsync(delegId, delegator);

        var (iCard, rCard) = await SeedInstancesAsync(scope, delegatee, recipient);
        await TradeSvc(scope).CreateAsync(tradeId, delegatee, recipient, iCard, rCard);

        Assert.True(await CanCancel(scope, delegator, tradeId));
    }

    [Fact]
    public async Task ActiveDelegator_CanCancel_WhenDelegateeIsRecipient()
    {
        using var scope = fixture.CreateScope();
        var delegator = UserId.New(); var delegatee = UserId.New(); var initiator = UserId.New();
        var delegId = DelegationId.New(); var tradeId = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        await DelegSvc(scope).ActivateAsync(delegId, delegator);

        var (iCard, rCard) = await SeedInstancesAsync(scope, initiator, delegatee);
        await TradeSvc(scope).CreateAsync(tradeId, initiator, delegatee, iCard, rCard);

        Assert.True(await CanCancel(scope, delegator, tradeId));
    }

    [Fact]
    public async Task ActiveDelegator_CancelAsync_DoesNotThrow()
    {
        using var scope = fixture.CreateScope();
        var delegator = UserId.New(); var delegatee = UserId.New(); var recipient = UserId.New();
        var delegId = DelegationId.New(); var tradeId = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        await DelegSvc(scope).ActivateAsync(delegId, delegator);

        var (iCard, rCard) = await SeedInstancesAsync(scope, delegatee, recipient);
        await TradeSvc(scope).CreateAsync(tradeId, delegatee, recipient, iCard, rCard);
        await TradeSvc(scope).CancelAsync(tradeId, delegator);

        var stored = await TradeRepo(scope).GetByIdAsync(tradeId);
        Assert.NotNull(stored);
        Assert.Equal(TradeProposalStatus.Cancelled, stored.Status);
    }

    [Fact]
    public async Task InactiveDelegation_DelegatorCannotCancel()
    {
        using var scope = fixture.CreateScope();
        var delegator = UserId.New(); var delegatee = UserId.New(); var recipient = UserId.New();
        var delegId = DelegationId.New(); var tradeId = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        // Intentionally NOT calling ActivateAsync

        var (iCard, rCard) = await SeedInstancesAsync(scope, delegatee, recipient);
        await TradeSvc(scope).CreateAsync(tradeId, delegatee, recipient, iCard, rCard);

        Assert.False(await CanCancel(scope, delegator, tradeId));
    }

    [Fact]
    public async Task AfterRevocation_DelegatorLosesCancelAccess()
    {
        using var scope = fixture.CreateScope();
        var delegator = UserId.New(); var delegatee = UserId.New(); var recipient = UserId.New();
        var delegId = DelegationId.New(); var tradeId = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        await DelegSvc(scope).ActivateAsync(delegId, delegator);

        var (iCard, rCard) = await SeedInstancesAsync(scope, delegatee, recipient);
        await TradeSvc(scope).CreateAsync(tradeId, delegatee, recipient, iCard, rCard);
        Assert.True(await CanCancel(scope, delegator, tradeId)); // confirm access before revoke

        await DelegSvc(scope).RevokeAsync(delegId, delegator);

        Assert.False(await CanCancel(scope, delegator, tradeId));
    }

    [Fact]
    public async Task UnrelatedDelegator_CannotCancel()
    {
        using var scope = fixture.CreateScope();
        var delegator = UserId.New(); var delegatee = UserId.New();
        var delegId = DelegationId.New(); var tradeId = TradeProposalId.New();

        await DelegSvc(scope).CreateAsync(delegId, delegator, delegatee);
        await DelegSvc(scope).ActivateAsync(delegId, delegator);

        // Trade does NOT involve delegatee — delegator gets no supervisor tuple.
        var stranger1 = UserId.New(); var stranger2 = UserId.New();
        var (iCard, rCard) = await SeedInstancesAsync(scope, stranger1, stranger2);
        await TradeSvc(scope).CreateAsync(tradeId, stranger1, stranger2, iCard, rCard);

        Assert.False(await CanCancel(scope, delegator, tradeId));
    }
}
