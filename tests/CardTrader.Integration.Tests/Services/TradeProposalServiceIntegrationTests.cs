using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.ValueObjects;
using CardTrader.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Integration.Tests.Services;

[Collection(IntegrationCollection.Name)]
public class TradeProposalServiceIntegrationTests(IntegrationFixture fixture)
{
    private TradeProposalService Service(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<TradeProposalService>();

    private IAuthorizationService Authz(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

    private Task<bool> CanView(IServiceScope scope, UserId user, TradeProposalId proposal) =>
        Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{user}",
            FgaRelations.CanView,
            $"{FgaTypes.TradeProposal}:{proposal}");

    private Task<bool> CanAccept(IServiceScope scope, UserId user, TradeProposalId proposal) =>
        Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{user}",
            FgaRelations.CanAccept,
            $"{FgaTypes.TradeProposal}:{proposal}");

    private Task<bool> CanCancel(IServiceScope scope, UserId user, TradeProposalId proposal) =>
        Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{user}",
            FgaRelations.CanCancel,
            $"{FgaTypes.TradeProposal}:{proposal}");

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_InitiatorCanCancel()
    {
        using var scope = fixture.CreateScope();
        var id = TradeProposalId.New();
        var initiator = UserId.New();
        var recipient = UserId.New();

        await Service(scope).CreateAsync(id, initiator, recipient);

        Assert.True(await CanCancel(scope, initiator, id));
        Assert.False(await CanCancel(scope, recipient, id));
    }

    [Fact]
    public async Task Create_RecipientCanAccept()
    {
        using var scope = fixture.CreateScope();
        var id = TradeProposalId.New();
        var initiator = UserId.New();
        var recipient = UserId.New();

        await Service(scope).CreateAsync(id, initiator, recipient);

        Assert.True(await CanAccept(scope, recipient, id));
        Assert.False(await CanAccept(scope, initiator, id));
    }

    // ── AcceptAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Accept_ByRecipient_Succeeds()
    {
        using var scope = fixture.CreateScope();
        var id = TradeProposalId.New();
        var initiator = UserId.New();
        var recipient = UserId.New();

        await Service(scope).CreateAsync(id, initiator, recipient);
        await Service(scope).AcceptAsync(id, recipient); // should not throw
    }

    [Fact]
    public async Task Accept_ByStranger_Throws()
    {
        using var scope = fixture.CreateScope();
        var id = TradeProposalId.New();
        var initiator = UserId.New();
        var recipient = UserId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(id, initiator, recipient);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Service(scope).AcceptAsync(id, stranger));
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_ByInitiator_Succeeds()
    {
        using var scope = fixture.CreateScope();
        var id = TradeProposalId.New();
        var initiator = UserId.New();
        var recipient = UserId.New();

        await Service(scope).CreateAsync(id, initiator, recipient);
        await Service(scope).CancelAsync(id, initiator); // should not throw
    }

    [Fact]
    public async Task Cancel_ByStranger_Throws()
    {
        using var scope = fixture.CreateScope();
        var id = TradeProposalId.New();
        var initiator = UserId.New();
        var recipient = UserId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(id, initiator, recipient);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Service(scope).CancelAsync(id, stranger));
    }

    // ── AssignFacilitator ─────────────────────────────────────────────────────

    [Fact]
    public async Task AssignFacilitator_FacilitatorCanViewButNotCancel()
    {
        using var scope = fixture.CreateScope();
        var id = TradeProposalId.New();
        var initiator = UserId.New();
        var recipient = UserId.New();
        var facilitator = UserId.New();

        await Service(scope).CreateAsync(id, initiator, recipient);
        await Service(scope).AssignFacilitatorAsync(id, initiator, facilitator);

        Assert.True(await CanView(scope, facilitator, id));
        Assert.False(await CanCancel(scope, facilitator, id));
        Assert.False(await CanAccept(scope, facilitator, id));
    }
}
