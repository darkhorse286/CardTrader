using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Entities;

public sealed class CardInstance : Entity<CardInstanceId>
{
    public CardId CardId { get; private init; }
    public UserId OwnerId { get; private init; }
    public int PrintNumber { get; private init; }

    private CardInstance() { }

    public CollectionId? CollectionId { get; private set; }

    public static CardInstance Create(CardInstanceId id, CardId cardId, UserId ownerId, int printNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(printNumber, 1);

        var instance = new CardInstance { Id = id, CardId = cardId, OwnerId = ownerId, PrintNumber = printNumber };
        instance.Raise(new CardInstanceCreated(id, cardId, ownerId));
        return instance;
    }

    public void AddToCollection(CollectionId collectionId)
    {
        CollectionId = collectionId;
        Raise(new CardInstanceAddedToCollection(Id, collectionId));
    }

    public void RemoveFromCollection(CollectionId collectionId)
    {
        CollectionId = null;
        Raise(new CardInstanceRemovedFromCollection(Id, collectionId));
    }
}
