using CardTrader.Application.Abstractions;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Services;

public sealed class DelegationService(
    IDelegationRepository delegations,
    IAuthorizationService authz,
    IDomainEventDispatcher dispatcher)
{
    public async Task<Delegation> CreateAsync(
        DelegationId id, UserId delegatorId, UserId delegateeId, CancellationToken ct = default)
    {
        var delegation = Delegation.Create(id, delegatorId, delegateeId);
        await delegations.AddAsync(delegation, ct);
        await dispatcher.DispatchAsync(delegation.DomainEvents, ct);
        delegation.ClearDomainEvents();
        return delegation;
    }

    public async Task ActivateAsync(
        DelegationId id, UserId requestingUserId, CancellationToken ct = default)
    {
        var delegation = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.Delegator, id, ct);
        delegation.Activate();
        await delegations.UpdateAsync(delegation, ct);
        await dispatcher.DispatchAsync(delegation.DomainEvents, ct);
        delegation.ClearDomainEvents();
    }

    public async Task ActivateWithExpiryAsync(
        DelegationId id, UserId requestingUserId, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        var delegation = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.Delegator, id, ct);
        delegation.ActivateWithExpiry(expiresAt);
        await delegations.UpdateAsync(delegation, ct);
        await dispatcher.DispatchAsync(delegation.DomainEvents, ct);
        delegation.ClearDomainEvents();
    }

    public async Task RevokeAsync(
        DelegationId id, UserId requestingUserId, CancellationToken ct = default)
    {
        var delegation = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.Delegator, id, ct);
        delegation.Revoke();
        await delegations.UpdateAsync(delegation, ct);
        await dispatcher.DispatchAsync(delegation.DomainEvents, ct);
        delegation.ClearDomainEvents();
    }

    private async Task<Delegation> GetOrThrowAsync(DelegationId id, CancellationToken ct)
        => await delegations.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Delegation {id} not found.");

    private Task CheckOrThrowAsync(UserId userId, string relation, DelegationId objectId, CancellationToken ct)
        => CheckOrThrowAsync(
            $"{FgaTypes.User}:{userId}", relation, $"{FgaTypes.Delegation}:{objectId}", ct);

    private async Task CheckOrThrowAsync(string user, string relation, string @object, CancellationToken ct)
    {
        if (!await authz.CheckAsync(user, relation, @object, ct))
            throw new UnauthorizedAccessException();
    }
}
