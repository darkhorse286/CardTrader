using CardTrader.Application.Services;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Enums;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using CardTrader.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Integration.Tests.Services;

[Collection(AdminIntegrationCollection.Name)]
public class AdminCancelTradeIntegrationTests(AdminIntegrationFixture fixture)
{
    private TradeProposalService TradeSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<TradeProposalService>();
    private CardInstanceService InstanceSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<CardInstanceService>();
    private ITradeProposalRepository Repo(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<ITradeProposalRepository>();

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

    [Fact]
    public async Task Cancel_ByAdmin_BypassesFgaAndCancels()
    {
        using var scope = fixture.CreateScope();
        var initiator = UserId.New(); var recipient = UserId.New();
        var (iCard, rCard) = await SeedInstancesAsync(scope, initiator, recipient);
        var tradeId = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(tradeId, initiator, recipient, iCard, rCard);

        // Admin cancels — no FGA can_cancel tuple needed.
        await TradeSvc(scope).CancelAsync(tradeId, fixture.AdminUserId);

        var stored = await Repo(scope).GetByIdAsync(tradeId);
        Assert.NotNull(stored);
        Assert.Equal(TradeProposalStatus.Cancelled, stored.Status);
    }

    [Fact]
    public async Task Cancel_ByNonAdmin_WithoutFga_Throws()
    {
        using var scope = fixture.CreateScope();
        var initiator = UserId.New(); var recipient = UserId.New();
        var (iCard, rCard) = await SeedInstancesAsync(scope, initiator, recipient);
        var tradeId = TradeProposalId.New();

        await TradeSvc(scope).CreateAsync(tradeId, initiator, recipient, iCard, rCard);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => TradeSvc(scope).CancelAsync(tradeId, UserId.New()));
    }
}
