using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using CardTrader.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Integration.Tests.Services;

[Collection(IntegrationCollection.Name)]
public class TradeProposalServiceIntegrationTests(IntegrationFixture fixture)
{
    private TradeProposalService TradeSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<TradeProposalService>();
    private CardInstanceService InstanceSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<CardInstanceService>();
    private IAuthorizationService Authz(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

    private Task<bool> CanView(IServiceScope scope, UserId user, TradeProposalId proposal) =>
        Authz(scope).CheckAsync($"{FgaTypes.User}:{user}", FgaRelations.CanView,
            $"{FgaTypes.TradeProposal}:{proposal}");
    private Task<bool> CanAccept(IServiceScope scope, UserId user, TradeProposalId proposal) =>
        Authz(scope).CheckAsync($"{FgaTypes.User}:{user}", FgaRelations.CanAccept,
            $"{FgaTypes.TradeProposal}:{proposal}");
    private Task<bool> CanCancel(IServiceScope scope, UserId user, TradeProposalId proposal) =>
        Authz(scope).CheckAsync($"{FgaTypes.User}:{user}", FgaRelations.CanCancel,
            $"{FgaTypes.TradeProposal}:{proposal}");

    // Creates one card and mints one instance per participant so FGA ownership is established.
    private async Task<(CardInstanceId InitiatorCard, CardInstanceId RecipientCard)>
        SeedInstancesAsync(IServiceScope scope, UserId initiator, UserId recipient)
    {
        var cardId = CardId.New();
        var cardRepo = scope.ServiceProvider.GetRequiredService<ICardRepository>();
        await cardRepo.AddAsync(Card.Create(cardId, "Trade Card", "Test Set", "Rare", "Player",
            printRun: 500));
        var iCard = CardInstanceId.New();
        var rCard = CardInstanceId.New();
        await InstanceSvc(scope).CreateAsync(iCard, cardId, initiator, 1);
        await InstanceSvc(scope).CreateAsync(rCard, cardId, recipient, 2);
        return (iCard, rCard);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_InitiatorCanCancel()
    {
        using var scope = fixture.CreateScope();
        var initiator = UserId.New(); var recipient = UserId.New();
        var (iCard, rCard) = await SeedInstancesAsync(scope, initiator, recipient);
        var id = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(id, initiator, recipient, iCard, rCard);

        Assert.True(await CanCancel(scope, initiator, id));
        Assert.False(await CanCancel(scope, recipient, id));
    }

    [Fact]
    public async Task Create_RecipientCanAccept()
    {
        using var scope = fixture.CreateScope();
        var initiator = UserId.New(); var recipient = UserId.New();
        var (iCard, rCard) = await SeedInstancesAsync(scope, initiator, recipient);
        var id = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(id, initiator, recipient, iCard, rCard);

        Assert.True(await CanAccept(scope, recipient, id));
        Assert.False(await CanAccept(scope, initiator, id));
    }

    // ── AcceptAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Accept_ByRecipient_Succeeds()
    {
        using var scope = fixture.CreateScope();
        var initiator = UserId.New(); var recipient = UserId.New();
        var (iCard, rCard) = await SeedInstancesAsync(scope, initiator, recipient);
        var id = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(id, initiator, recipient, iCard, rCard);
        await TradeSvc(scope).AcceptAsync(id, recipient); // should not throw
    }

    [Fact]
    public async Task Accept_ByStranger_Throws()
    {
        using var scope = fixture.CreateScope();
        var initiator = UserId.New(); var recipient = UserId.New();
        var (iCard, rCard) = await SeedInstancesAsync(scope, initiator, recipient);
        var id = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(id, initiator, recipient, iCard, rCard);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => TradeSvc(scope).AcceptAsync(id, UserId.New()));
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_ByInitiator_Succeeds()
    {
        using var scope = fixture.CreateScope();
        var initiator = UserId.New(); var recipient = UserId.New();
        var (iCard, rCard) = await SeedInstancesAsync(scope, initiator, recipient);
        var id = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(id, initiator, recipient, iCard, rCard);
        await TradeSvc(scope).CancelAsync(id, initiator); // should not throw
    }

    [Fact]
    public async Task Cancel_ByStranger_Throws()
    {
        using var scope = fixture.CreateScope();
        var initiator = UserId.New(); var recipient = UserId.New();
        var (iCard, rCard) = await SeedInstancesAsync(scope, initiator, recipient);
        var id = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(id, initiator, recipient, iCard, rCard);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => TradeSvc(scope).CancelAsync(id, UserId.New()));
    }

    // ── AssignFacilitator ─────────────────────────────────────────────────────

    [Fact]
    public async Task AssignFacilitator_FacilitatorCanViewButNotCancel()
    {
        using var scope = fixture.CreateScope();
        var initiator = UserId.New(); var recipient = UserId.New();
        var (iCard, rCard) = await SeedInstancesAsync(scope, initiator, recipient);
        var facilitator = UserId.New();
        var id = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(id, initiator, recipient, iCard, rCard);
        await TradeSvc(scope).AssignFacilitatorAsync(id, initiator, facilitator);

        Assert.True(await CanView(scope, facilitator, id));
        Assert.False(await CanCancel(scope, facilitator, id));
        Assert.False(await CanAccept(scope, facilitator, id));
    }
}
