using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;

namespace CardTrader.Authorization.Tests.Adversarial;

[Collection(OpenFgaCollection.Name)]
public sealed class DealerBoundaryTests(OpenFgaFixture fixture)
    : FgaTestBase(fixture)
{
    // ── Scenario F: Dealer (facilitator) access boundaries ───────────────────

    [Fact]
    public async Task Facilitator_CanViewAssignedTradeProposal()
    {
        var (alice, bob, dealer, proposal) = (Id(), Id(), Id(), Id());
        await SetupProposalWithDealer(alice, bob, dealer, proposal);

        Assert.True(await CanAsync(
            $"user:{dealer}", FgaRelations.CanView, $"{FgaTypes.TradeProposal}:{proposal}"));
    }

    [Fact]
    public async Task Facilitator_CannotAcceptTradeProposal()
    {
        // Only the recipient may accept.
        var (alice, bob, dealer, proposal) = (Id(), Id(), Id(), Id());
        await SetupProposalWithDealer(alice, bob, dealer, proposal);

        Assert.False(await CanAsync(
            $"user:{dealer}", FgaRelations.CanAccept, $"{FgaTypes.TradeProposal}:{proposal}"));
    }

    [Fact]
    public async Task Facilitator_CannotCancelTradeProposal()
    {
        // Only the initiator or a supervisor may cancel.
        var (alice, bob, dealer, proposal) = (Id(), Id(), Id(), Id());
        await SetupProposalWithDealer(alice, bob, dealer, proposal);

        Assert.False(await CanAsync(
            $"user:{dealer}", FgaRelations.CanCancel, $"{FgaTypes.TradeProposal}:{proposal}"));
    }

    [Fact]
    public async Task Initiator_CanViewAndCancelOwnProposal()
    {
        var (alice, bob, dealer, proposal) = (Id(), Id(), Id(), Id());
        await SetupProposalWithDealer(alice, bob, dealer, proposal);

        Assert.True(await CanAsync($"user:{alice}", FgaRelations.CanView,   $"{FgaTypes.TradeProposal}:{proposal}"));
        Assert.True(await CanAsync($"user:{alice}", FgaRelations.CanCancel, $"{FgaTypes.TradeProposal}:{proposal}"));
        Assert.False(await CanAsync($"user:{alice}", FgaRelations.CanAccept, $"{FgaTypes.TradeProposal}:{proposal}"));
    }

    [Fact]
    public async Task Recipient_CanViewAndAcceptProposal_CannotCancel()
    {
        var (alice, bob, dealer, proposal) = (Id(), Id(), Id(), Id());
        await SetupProposalWithDealer(alice, bob, dealer, proposal);

        Assert.True(await CanAsync($"user:{bob}", FgaRelations.CanView,   $"{FgaTypes.TradeProposal}:{proposal}"));
        Assert.True(await CanAsync($"user:{bob}", FgaRelations.CanAccept, $"{FgaTypes.TradeProposal}:{proposal}"));
        Assert.False(await CanAsync($"user:{bob}", FgaRelations.CanCancel, $"{FgaTypes.TradeProposal}:{proposal}"));
    }

    [Fact]
    public async Task UnrelatedUser_CannotViewTradeProposal()
    {
        var (alice, bob, dealer, proposal, eve) = (Id(), Id(), Id(), Id(), Id());
        await SetupProposalWithDealer(alice, bob, dealer, proposal);

        Assert.False(await CanAsync($"user:{eve}", FgaRelations.CanView, $"{FgaTypes.TradeProposal}:{proposal}"));
    }

    [Fact]
    public async Task UnassignedDealer_CannotViewTradeProposal()
    {
        // A dealer not assigned as facilitator on this specific proposal has no access.
        var (alice, bob, dealer, otherDealer, proposal) = (Id(), Id(), Id(), Id(), Id());
        await SetupProposalWithDealer(alice, bob, dealer, proposal);

        Assert.False(await CanAsync(
            $"user:{otherDealer}", FgaRelations.CanView, $"{FgaTypes.TradeProposal}:{proposal}"));
    }

    // ── Setup helper ─────────────────────────────────────────────────────────

    private async Task SetupProposalWithDealer(
        string initiator, string recipient, string dealer, string proposalId)
    {
        await WriteAsync(
            T($"user:{initiator}", FgaRelations.Initiator,   $"{FgaTypes.TradeProposal}:{proposalId}"),
            T($"user:{recipient}", FgaRelations.Recipient,   $"{FgaTypes.TradeProposal}:{proposalId}"),
            T($"user:{dealer}",    FgaRelations.Facilitator, $"{FgaTypes.TradeProposal}:{proposalId}"));
    }
}
