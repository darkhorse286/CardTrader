using CardTrader.Domain.Common;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Events;

public sealed record TradeProposalCreated(
    TradeProposalId Id, UserId InitiatorId, UserId RecipientId) : IDomainEvent;

public sealed record TradeProposalFacilitatorAssigned(
    TradeProposalId Id, UserId FacilitatorId) : IDomainEvent;

public sealed record TradeProposalAccepted(TradeProposalId Id) : IDomainEvent;

public sealed record TradeProposalCancelled(TradeProposalId Id) : IDomainEvent;
