using CardTrader.Application.Abstractions;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Services;

public sealed class RosterService(
    IRosterRepository rosters,
    IAuthorizationService authz,
    IDomainEventDispatcher dispatcher)
{
    public Task<Roster?> GetByIdAsync(RosterId id, CancellationToken ct = default)
        => rosters.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<Roster>> GetOwnedAsync(UserId ownerId, CancellationToken ct = default)
        => rosters.GetAllByOwnerAsync(ownerId, ct);

    public async Task<Roster> CreateAsync(
        RosterId id, string name, UserId ownerId, CancellationToken ct = default)
    {
        var roster = Roster.Create(id, name, ownerId);
        await rosters.AddAsync(roster, ct);
        await dispatcher.DispatchAsync(roster.DomainEvents, ct);
        roster.ClearDomainEvents();
        return roster;
    }

    // Share/Unshare/MakePublic/MakePrivate only write FGA tuples — the Roster row in the DB
    // does not change, so no UpdateAsync is needed.

    public async Task ShareWithUserAsync(
        RosterId id, UserId requestingUserId, UserId targetUserId, CancellationToken ct = default)
    {
        var roster = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        roster.ShareWithUser(targetUserId);
        await dispatcher.DispatchAsync(roster.DomainEvents, ct);
        roster.ClearDomainEvents();
    }

    public async Task UnshareAsync(
        RosterId id, UserId requestingUserId, UserId targetUserId, CancellationToken ct = default)
    {
        var roster = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        roster.Unshare(targetUserId);
        await dispatcher.DispatchAsync(roster.DomainEvents, ct);
        roster.ClearDomainEvents();
    }

    public async Task MakePublicAsync(
        RosterId id, UserId requestingUserId, CancellationToken ct = default)
    {
        var roster = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        roster.MakePublic();
        await dispatcher.DispatchAsync(roster.DomainEvents, ct);
        roster.ClearDomainEvents();
    }

    public async Task MakePrivateAsync(
        RosterId id, UserId requestingUserId, CancellationToken ct = default)
    {
        var roster = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        roster.MakePrivate();
        await dispatcher.DispatchAsync(roster.DomainEvents, ct);
        roster.ClearDomainEvents();
    }

    private async Task<Roster> GetOrThrowAsync(RosterId id, CancellationToken ct)
        => await rosters.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Roster {id} not found.");

    private Task CheckOrThrowAsync(UserId userId, string relation, RosterId objectId, CancellationToken ct)
        => CheckOrThrowAsync(
            $"{FgaTypes.User}:{userId}", relation, $"{FgaTypes.Roster}:{objectId}", ct);

    private async Task CheckOrThrowAsync(string user, string relation, string @object, CancellationToken ct)
    {
        if (!await authz.CheckAsync(user, relation, @object, ct))
            throw new UnauthorizedAccessException();
    }
}
