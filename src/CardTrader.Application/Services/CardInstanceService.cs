using CardTrader.Application.Abstractions;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Services;

public sealed class CardInstanceService(
    ICardInstanceRepository instances,
    IAuthorizationService authz,
    IDomainEventDispatcher dispatcher)
{
    public async Task<CardInstance> CreateAsync(
        CardInstanceId id, CardId cardId, UserId ownerId, int printNumber, CancellationToken ct = default)
    {
        var instance = CardInstance.Create(id, cardId, ownerId, printNumber);
        await instances.AddAsync(instance, ct);
        await dispatcher.DispatchAsync(instance.DomainEvents, ct);
        instance.ClearDomainEvents();
        return instance;
    }

    public async Task AddToCollectionAsync(
        CardInstanceId id, UserId requestingUserId, CollectionId collectionId, CancellationToken ct = default)
    {
        var instance = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        instance.AddToCollection(collectionId);
        await dispatcher.DispatchAsync(instance.DomainEvents, ct);
        instance.ClearDomainEvents();
    }

    public async Task RemoveFromCollectionAsync(
        CardInstanceId id, UserId requestingUserId, CollectionId collectionId, CancellationToken ct = default)
    {
        var instance = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        instance.RemoveFromCollection(collectionId);
        await dispatcher.DispatchAsync(instance.DomainEvents, ct);
        instance.ClearDomainEvents();
    }

    private async Task<CardInstance> GetOrThrowAsync(CardInstanceId id, CancellationToken ct)
        => await instances.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"CardInstance {id} not found.");

    private Task CheckOrThrowAsync(UserId userId, string relation, CardInstanceId objectId, CancellationToken ct)
        => CheckOrThrowAsync(
            $"{FgaTypes.User}:{userId}", relation, $"{FgaTypes.CardInstance}:{objectId}", ct);

    private async Task CheckOrThrowAsync(string user, string relation, string @object, CancellationToken ct)
    {
        if (!await authz.CheckAsync(user, relation, @object, ct))
            throw new UnauthorizedAccessException();
    }
}
