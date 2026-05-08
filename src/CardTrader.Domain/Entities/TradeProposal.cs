using CardTrader.Domain.Common;
using CardTrader.Domain.Enums;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Entities;

public sealed class TradeProposal : Entity<TradeProposalId>
{
    public UserId InitiatorId { get; private init; }
    public UserId RecipientId { get; private init; }
    public TradeProposalStatus Status { get; private set; }

    private TradeProposal() { }

    public static TradeProposal Create(TradeProposalId id, UserId initiatorId, UserId recipientId)
    {
        if (initiatorId == recipientId)
            throw new ArgumentException("Initiator and recipient must be different users.");

        var proposal = new TradeProposal
        {
            Id = id,
            InitiatorId = initiatorId,
            RecipientId = recipientId,
            Status = TradeProposalStatus.Pending,
        };
        proposal.Raise(new TradeProposalCreated(id, initiatorId, recipientId));
        return proposal;
    }

    public void AssignFacilitator(UserId facilitatorId)
    {
        if (Status != TradeProposalStatus.Pending)
            throw new InvalidOperationException("Can only assign a facilitator to a pending proposal.");

        Raise(new TradeProposalFacilitatorAssigned(Id, facilitatorId));
    }

    public void Accept()
    {
        if (Status != TradeProposalStatus.Pending)
            throw new InvalidOperationException("Only a pending proposal can be accepted.");

        Status = TradeProposalStatus.Accepted;
        Raise(new TradeProposalAccepted(Id));
    }

    public void Cancel()
    {
        if (Status != TradeProposalStatus.Pending)
            throw new InvalidOperationException("Only a pending proposal can be cancelled.");

        Status = TradeProposalStatus.Cancelled;
        Raise(new TradeProposalCancelled(Id));
    }
}
