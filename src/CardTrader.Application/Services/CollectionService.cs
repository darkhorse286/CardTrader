using CardTrader.Application.Abstractions;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Services;

public sealed class CollectionService(
    ICollectionRepository collections,
    IAuthorizationService authz,
    IDomainEventDispatcher dispatcher)
{
    public async Task<Collection> CreateAsync(
        CollectionId id, string name, UserId ownerId, CancellationToken ct = default)
    {
        var collection = Collection.Create(id, name, ownerId);
        await collections.AddAsync(collection, ct);
        await dispatcher.DispatchAsync(collection.DomainEvents, ct);
        collection.ClearDomainEvents();
        return collection;
    }

    // Share/Unshare/MakePublic/MakePrivate only write FGA tuples — the Collection row in the DB
    // does not change, so no UpdateAsync is needed.

    public async Task ShareWithUserAsync(
        CollectionId id, UserId requestingUserId, UserId targetUserId, CancellationToken ct = default)
    {
        var collection = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        collection.ShareWithUser(targetUserId);
        await dispatcher.DispatchAsync(collection.DomainEvents, ct);
        collection.ClearDomainEvents();
    }

    public async Task UnshareAsync(
        CollectionId id, UserId requestingUserId, UserId targetUserId, CancellationToken ct = default)
    {
        var collection = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        collection.Unshare(targetUserId);
        await dispatcher.DispatchAsync(collection.DomainEvents, ct);
        collection.ClearDomainEvents();
    }

    public async Task MakePublicAsync(
        CollectionId id, UserId requestingUserId, CancellationToken ct = default)
    {
        var collection = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        collection.MakePublic();
        await dispatcher.DispatchAsync(collection.DomainEvents, ct);
        collection.ClearDomainEvents();
    }

    public async Task MakePrivateAsync(
        CollectionId id, UserId requestingUserId, CancellationToken ct = default)
    {
        var collection = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        collection.MakePrivate();
        await dispatcher.DispatchAsync(collection.DomainEvents, ct);
        collection.ClearDomainEvents();
    }

    private async Task<Collection> GetOrThrowAsync(CollectionId id, CancellationToken ct)
        => await collections.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Collection {id} not found.");

    private Task CheckOrThrowAsync(UserId userId, string relation, CollectionId objectId, CancellationToken ct)
        => CheckOrThrowAsync(
            $"{FgaTypes.User}:{userId}", relation, $"{FgaTypes.Collection}:{objectId}", ct);

    private async Task CheckOrThrowAsync(string user, string relation, string @object, CancellationToken ct)
    {
        if (!await authz.CheckAsync(user, relation, @object, ct))
            throw new UnauthorizedAccessException();
    }
}
