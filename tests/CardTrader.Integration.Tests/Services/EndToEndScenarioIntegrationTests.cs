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
/// End-to-end scenario tests covering the seeded demo flow:
/// alice and bob as regular users, alice proposes a trade, bob accepts,
/// bob has a public roster alice can view but not manage.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class EndToEndScenarioIntegrationTests(IntegrationFixture fixture)
{
    private CardInstanceService InstanceSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<CardInstanceService>();
    private RosterService RosterSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<RosterService>();
    private TradeProposalService TradeSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<TradeProposalService>();
    private IAuthorizationService Authz(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    private ITradeProposalRepository TradeRepo(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<ITradeProposalRepository>();
    private ICardInstanceRepository InstanceRepo(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<ICardInstanceRepository>();

    private async Task<CardId> SeedCardAsync(IServiceScope scope)
    {
        var cardId = CardId.New();
        var repo = scope.ServiceProvider.GetRequiredService<ICardRepository>();
        await repo.AddAsync(Card.Create(cardId, "Demo Card", "Demo Set", "Rare", "Demo Player",
            printRun: 500, playerPosition: "Attack"));
        return cardId;
    }

    private async Task<(CardInstanceId AliceCard, CardInstanceId BobCard)>
        SeedTradeInstancesAsync(IServiceScope scope, UserId alice, UserId bob)
    {
        var cardId = await SeedCardAsync(scope);
        var aCard = CardInstanceId.New();
        var bCard = CardInstanceId.New();
        await InstanceSvc(scope).CreateAsync(aCard, cardId, alice, 1);
        await InstanceSvc(scope).CreateAsync(bCard, cardId, bob, 2);
        return (aCard, bCard);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Bob's public roster is visible to alice (can_view) but alice cannot manage it
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BobPublicRoster_AliceCanView_CannotManage()
    {
        using var scope = fixture.CreateScope();
        var alice = UserId.New(); var bob = UserId.New();
        var bobRosterId = RosterId.New();

        await RosterSvc(scope).CreateAsync(bobRosterId, "Bob's Biscuits", bob);
        await RosterSvc(scope).MakePublicAsync(bobRosterId, bob);

        Assert.True(await Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{alice}", FgaRelations.CanView, $"{FgaTypes.Roster}:{bobRosterId}"));
        Assert.False(await Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{alice}", FgaRelations.CanManage, $"{FgaTypes.Roster}:{bobRosterId}"));
    }

    [Fact]
    public async Task BobPublicRoster_AppearsInAliceVisibleList()
    {
        using var scope = fixture.CreateScope();
        var alice = UserId.New(); var bob = UserId.New();
        var bobRosterId = RosterId.New();

        await RosterSvc(scope).CreateAsync(bobRosterId, "Bob's Biscuits", bob);
        await RosterSvc(scope).MakePublicAsync(bobRosterId, bob);

        var aliceVisible = await RosterSvc(scope).GetAllVisibleAsync(alice);

        Assert.Contains(aliceVisible, r => r.Id == bobRosterId);
    }

    [Fact]
    public async Task BobPublicRoster_NotInAliceOwnedList()
    {
        using var scope = fixture.CreateScope();
        var alice = UserId.New(); var bob = UserId.New();
        var bobRosterId = RosterId.New();

        await RosterSvc(scope).CreateAsync(bobRosterId, "Bob's Biscuits", bob);
        await RosterSvc(scope).MakePublicAsync(bobRosterId, bob);

        var aliceOwned = await RosterSvc(scope).GetOwnedAsync(alice);

        Assert.DoesNotContain(aliceOwned, r => r.Id == bobRosterId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Alice proposes a trade to bob — bob accepts
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Alice_ProposesToBob_BobCanAcceptAliceCannot()
    {
        using var scope = fixture.CreateScope();
        var alice = UserId.New(); var bob = UserId.New();
        var (aCard, bCard) = await SeedTradeInstancesAsync(scope, alice, bob);
        var tradeId = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(tradeId, alice, bob, aCard, bCard);

        Assert.True(await Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{bob}", FgaRelations.CanAccept, $"{FgaTypes.TradeProposal}:{tradeId}"));
        Assert.False(await Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{alice}", FgaRelations.CanAccept, $"{FgaTypes.TradeProposal}:{tradeId}"));
    }

    [Fact]
    public async Task Alice_ProposesToBob_AliceCanCancelBobCannot()
    {
        using var scope = fixture.CreateScope();
        var alice = UserId.New(); var bob = UserId.New();
        var (aCard, bCard) = await SeedTradeInstancesAsync(scope, alice, bob);
        var tradeId = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(tradeId, alice, bob, aCard, bCard);

        Assert.True(await Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{alice}", FgaRelations.CanCancel, $"{FgaTypes.TradeProposal}:{tradeId}"));
        Assert.False(await Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{bob}", FgaRelations.CanCancel, $"{FgaTypes.TradeProposal}:{tradeId}"));
    }

    [Fact]
    public async Task Bob_AcceptsTrade_StatusBecomesAccepted()
    {
        using var scope = fixture.CreateScope();
        var alice = UserId.New(); var bob = UserId.New();
        var (aCard, bCard) = await SeedTradeInstancesAsync(scope, alice, bob);
        var tradeId = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(tradeId, alice, bob, aCard, bCard);
        await TradeSvc(scope).AcceptAsync(tradeId, bob);

        var stored = await TradeRepo(scope).GetByIdAsync(tradeId);
        Assert.NotNull(stored);
        Assert.Equal(TradeProposalStatus.Accepted, stored.Status);
    }

    [Fact]
    public async Task Bob_AcceptsTrade_OwnershipSwaps()
    {
        // After acceptance: alice now owns bCard and bob now owns aCard.
        using var scope = fixture.CreateScope();
        var alice = UserId.New(); var bob = UserId.New();
        var (aCard, bCard) = await SeedTradeInstancesAsync(scope, alice, bob);
        var tradeId = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(tradeId, alice, bob, aCard, bCard);
        await TradeSvc(scope).AcceptAsync(tradeId, bob);

        var aliceInstance = await InstanceRepo(scope).GetByIdAsync(aCard);
        var bobInstance   = await InstanceRepo(scope).GetByIdAsync(bCard);
        Assert.NotNull(aliceInstance); Assert.NotNull(bobInstance);
        Assert.Equal(bob,   aliceInstance.OwnerId); // alice's card now belongs to bob
        Assert.Equal(alice, bobInstance.OwnerId);   // bob's card now belongs to alice
    }

    [Fact]
    public async Task Bob_AcceptTrade_AliceAttemptThrows()
    {
        using var scope = fixture.CreateScope();
        var alice = UserId.New(); var bob = UserId.New();
        var (aCard, bCard) = await SeedTradeInstancesAsync(scope, alice, bob);
        var tradeId = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(tradeId, alice, bob, aCard, bCard);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => TradeSvc(scope).AcceptAsync(tradeId, alice));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Alice's owned card instances appear in GetOwnedAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Alice_OwnedInstances_AppearsInGetOwnedAsync()
    {
        using var scope = fixture.CreateScope();
        var alice = UserId.New();
        var cardId = await SeedCardAsync(scope);
        var instanceId = CardInstanceId.New();

        await InstanceSvc(scope).CreateAsync(instanceId, cardId, alice, printNumber: 1);

        var owned = await InstanceSvc(scope).GetOwnedAsync(alice);

        Assert.Single(owned);
        Assert.Equal(instanceId, owned[0].Id);
    }
}
