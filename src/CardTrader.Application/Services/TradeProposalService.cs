using CardTrader.Application.Abstractions;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Services;

public sealed class TradeProposalService(
    ITradeProposalRepository proposals,
    ICardInstanceRepository instances,
    IAuthorizationService authz,
    IAdminService admin,
    IDomainEventDispatcher dispatcher)
{
    public Task<IReadOnlyList<TradeProposal>> GetInvolvingUserAsync(UserId userId, CancellationToken ct = default)
        => proposals.GetInvolvingUserAsync(userId, ct);

    public async Task<IReadOnlyList<TradeProposal>> GetAllPendingAsync(
        UserId requestingUserId, CancellationToken ct = default)
    {
        if (!await admin.IsAdminAsync(requestingUserId, ct))
            throw new UnauthorizedAccessException("Only admins can view all pending trades.");
        return await proposals.GetAllPendingAsync(ct);
    }

    public async Task<TradeProposal> CreateAsync(
        TradeProposalId id,
        UserId initiatorId, UserId recipientId,
        CardInstanceId initiatorInstanceId, CardInstanceId recipientInstanceId,
        CancellationToken ct = default)
    {
        // Verify each party owns the card they're offering
        await CheckOrThrowAsync(
            $"{FgaTypes.User}:{initiatorId}", FgaRelations.CanTrade,
            $"{FgaTypes.CardInstance}:{initiatorInstanceId}", ct,
            "You don't own the card you're offering.");

        await CheckOrThrowAsync(
            $"{FgaTypes.User}:{recipientId}", FgaRelations.CanTrade,
            $"{FgaTypes.CardInstance}:{recipientInstanceId}", ct,
            "The recipient doesn't own the card you're requesting.");

        var proposal = TradeProposal.Create(id, initiatorId, recipientId,
            initiatorInstanceId, recipientInstanceId);
        await proposals.AddAsync(proposal, ct);
        await dispatcher.DispatchAsync(proposal.DomainEvents, ct);
        proposal.ClearDomainEvents();
        return proposal;
    }

    // Initiator or supervisor can assign a facilitator (same parties who can cancel).
    public async Task AssignFacilitatorAsync(
        TradeProposalId id, UserId requestingUserId, UserId facilitatorId, CancellationToken ct = default)
    {
        var proposal = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanCancel, id, ct);
        proposal.AssignFacilitator(facilitatorId);
        await dispatcher.DispatchAsync(proposal.DomainEvents, ct);
        proposal.ClearDomainEvents();
    }

    public async Task AcceptAsync(
        TradeProposalId id, UserId requestingUserId, CancellationToken ct = default)
    {
        var proposal = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanAccept, id, ct);
        proposal.Accept();
        await proposals.UpdateAsync(proposal, ct);
        await dispatcher.DispatchAsync(proposal.DomainEvents, ct);
        proposal.ClearDomainEvents();

        // Settle: swap ownership of both card instances
        await SwapOwnershipAsync(proposal, ct);
    }

    public async Task CancelAsync(
        TradeProposalId id, UserId requestingUserId, CancellationToken ct = default)
    {
        var proposal = await GetOrThrowAsync(id, ct);
        var isAdmin = await admin.IsAdminAsync(requestingUserId, ct);
        if (!isAdmin)
            await CheckOrThrowAsync(requestingUserId, FgaRelations.CanCancel, id, ct);
        proposal.Cancel();
        await proposals.UpdateAsync(proposal, ct);
        await dispatcher.DispatchAsync(proposal.DomainEvents, ct);
        proposal.ClearDomainEvents();
    }

    private async Task SwapOwnershipAsync(TradeProposal proposal, CancellationToken ct)
    {
        var initiatorCard = await instances.GetByIdAsync(proposal.InitiatorInstanceId, ct)
            ?? throw new InvalidOperationException("Initiator's card instance not found.");
        var recipientCard = await instances.GetByIdAsync(proposal.RecipientInstanceId, ct)
            ?? throw new InvalidOperationException("Recipient's card instance not found.");

        // Initiator's card goes to the recipient, and vice versa
        initiatorCard.TransferOwnership(proposal.RecipientId);
        recipientCard.TransferOwnership(proposal.InitiatorId);

        await instances.UpdateAsync(initiatorCard, ct);
        await instances.UpdateAsync(recipientCard, ct);

        await dispatcher.DispatchAsync([.. initiatorCard.DomainEvents, .. recipientCard.DomainEvents], ct);
        initiatorCard.ClearDomainEvents();
        recipientCard.ClearDomainEvents();
    }

    private async Task<TradeProposal> GetOrThrowAsync(TradeProposalId id, CancellationToken ct)
        => await proposals.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"TradeProposal {id} not found.");

    private Task CheckOrThrowAsync(UserId userId, string relation, TradeProposalId objectId, CancellationToken ct)
        => CheckOrThrowAsync(
            $"{FgaTypes.User}:{userId}", relation, $"{FgaTypes.TradeProposal}:{objectId}", ct);

    private async Task CheckOrThrowAsync(
        string user, string relation, string @object, CancellationToken ct,
        string? message = null)
    {
        if (!await authz.CheckAsync(user, relation, @object, ct))
            throw new UnauthorizedAccessException(message);
    }
}
