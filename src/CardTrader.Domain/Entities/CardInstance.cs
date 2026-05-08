using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Entities;

public sealed class CardInstance : Entity<CardInstanceId>
{
    public CardId CardId { get; private init; }
    public UserId OwnerId { get; private init; }

    private CardInstance() { }

    public static CardInstance Create(CardInstanceId id, CardId cardId, UserId ownerId)
    {
        var instance = new CardInstance { Id = id, CardId = cardId, OwnerId = ownerId };
        instance.Raise(new CardInstanceCreated(id, cardId, ownerId));
        return instance;
    }

    public void AddToCollection(CollectionId collectionId)
        => Raise(new CardInstanceAddedToCollection(Id, collectionId));

    public void RemoveFromCollection(CollectionId collectionId)
        => Raise(new CardInstanceRemovedFromCollection(Id, collectionId));
}
