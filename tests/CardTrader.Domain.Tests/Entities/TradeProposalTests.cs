using CardTrader.Domain.Entities;
using CardTrader.Domain.Enums;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Tests.Entities;

public sealed class TradeProposalTests
{
    private static readonly TradeProposalId Id    = TradeProposalId.New();
    private static readonly UserId Alice          = UserId.New();
    private static readonly UserId Bob            = UserId.New();
    private static readonly UserId Dealer         = UserId.New();
    private static readonly CardInstanceId AliceCard = CardInstanceId.New();
    private static readonly CardInstanceId BobCard   = CardInstanceId.New();

    private static TradeProposal Make() =>
        TradeProposal.Create(Id, Alice, Bob, AliceCard, BobCard);

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsProperties()
    {
        var proposal = Make();

        Assert.Equal(Id, proposal.Id);
        Assert.Equal(Alice, proposal.InitiatorId);
        Assert.Equal(Bob, proposal.RecipientId);
        Assert.Equal(AliceCard, proposal.InitiatorInstanceId);
        Assert.Equal(BobCard, proposal.RecipientInstanceId);
        Assert.Equal(TradeProposalStatus.Pending, proposal.Status);
    }

    [Fact]
    public void Create_RaisesTradeProposalCreated()
    {
        var proposal = Make();

        var evt = Assert.Single(proposal.DomainEvents);
        var created = Assert.IsType<TradeProposalCreated>(evt);
        Assert.Equal(Id, created.Id);
        Assert.Equal(Alice, created.InitiatorId);
        Assert.Equal(Bob, created.RecipientId);
        Assert.Equal(AliceCard, created.InitiatorInstanceId);
        Assert.Equal(BobCard, created.RecipientInstanceId);
    }

    [Fact]
    public void Create_SameInitiatorAndRecipient_Throws()
        => Assert.Throws<ArgumentException>(() =>
            TradeProposal.Create(Id, Alice, Alice, AliceCard, BobCard));

    // ── AssignFacilitator ────────────────────────────────────────────────────

    [Fact]
    public void AssignFacilitator_OnPending_RaisesEvent()
    {
        var proposal = Make();
        proposal.ClearDomainEvents();

        proposal.AssignFacilitator(Dealer);

        var evt = Assert.Single(proposal.DomainEvents);
        var assigned = Assert.IsType<TradeProposalFacilitatorAssigned>(evt);
        Assert.Equal(Id, assigned.Id);
        Assert.Equal(Dealer, assigned.FacilitatorId);
    }

    [Fact]
    public void AssignFacilitator_OnAccepted_Throws()
    {
        var proposal = Make();
        proposal.Accept();

        Assert.Throws<InvalidOperationException>(() => proposal.AssignFacilitator(Dealer));
    }

    [Fact]
    public void AssignFacilitator_OnCancelled_Throws()
    {
        var proposal = Make();
        proposal.Cancel();

        Assert.Throws<InvalidOperationException>(() => proposal.AssignFacilitator(Dealer));
    }

    // ── Accept ───────────────────────────────────────────────────────────────

    [Fact]
    public void Accept_OnPending_SetsStatusAndRaisesEvent()
    {
        var proposal = Make();
        proposal.ClearDomainEvents();

        proposal.Accept();

        Assert.Equal(TradeProposalStatus.Accepted, proposal.Status);
        var evt = Assert.Single(proposal.DomainEvents);
        var accepted = Assert.IsType<TradeProposalAccepted>(evt);
        Assert.Equal(Id, accepted.Id);
    }

    [Fact]
    public void Accept_OnAccepted_Throws()
    {
        var proposal = Make();
        proposal.Accept();

        Assert.Throws<InvalidOperationException>(() => proposal.Accept());
    }

    [Fact]
    public void Accept_OnCancelled_Throws()
    {
        var proposal = Make();
        proposal.Cancel();

        Assert.Throws<InvalidOperationException>(() => proposal.Accept());
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_OnPending_SetsStatusAndRaisesEvent()
    {
        var proposal = Make();
        proposal.ClearDomainEvents();

        proposal.Cancel();

        Assert.Equal(TradeProposalStatus.Cancelled, proposal.Status);
        var evt = Assert.Single(proposal.DomainEvents);
        var cancelled = Assert.IsType<TradeProposalCancelled>(evt);
        Assert.Equal(Id, cancelled.Id);
    }

    [Fact]
    public void Cancel_OnAccepted_Throws()
    {
        var proposal = Make();
        proposal.Accept();

        Assert.Throws<InvalidOperationException>(() => proposal.Cancel());
    }

    [Fact]
    public void Cancel_OnCancelled_Throws()
    {
        var proposal = Make();
        proposal.Cancel();

        Assert.Throws<InvalidOperationException>(() => proposal.Cancel());
    }
}
