using CardTrader.Application.Services;
using CardTrader.Domain.Enums;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using CardTrader.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Integration.Tests.Services;

[Collection(AdminIntegrationCollection.Name)]
public class AdminCancelTradeIntegrationTests(AdminIntegrationFixture fixture)
{
    private TradeProposalService Service(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<TradeProposalService>();

    private ITradeProposalRepository Repo(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<ITradeProposalRepository>();

    [Fact]
    public async Task Cancel_ByAdmin_BypassesFgaAndCancels()
    {
        // Arrange: create a trade between two users the admin has no FGA relation to.
        using var scope = fixture.CreateScope();
        var tradeId = TradeProposalId.New();
        var initiator = UserId.New();
        var recipient = UserId.New();

        await Service(scope).CreateAsync(tradeId, initiator, recipient);

        // Act: admin cancels — should not throw despite having no FGA can_cancel tuple.
        await Service(scope).CancelAsync(tradeId, fixture.AdminUserId);

        // Assert: trade is persisted as Cancelled.
        var stored = await Repo(scope).GetByIdAsync(tradeId);
        Assert.NotNull(stored);
        Assert.Equal(TradeProposalStatus.Cancelled, stored.Status);
    }

    [Fact]
    public async Task Cancel_ByNonAdmin_WithoutFga_Throws()
    {
        // Validates the admin bypass is not granted to arbitrary users.
        using var scope = fixture.CreateScope();
        var tradeId = TradeProposalId.New();
        var initiator = UserId.New();
        var recipient = UserId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(tradeId, initiator, recipient);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Service(scope).CancelAsync(tradeId, stranger));
    }
}
