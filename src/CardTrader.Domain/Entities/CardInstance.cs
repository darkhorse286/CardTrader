using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Entities;

public sealed class CardInstance : Entity<CardInstanceId>
{
    public CardId CardId { get; private init; }
    public UserId OwnerId { get; private set; }
    public int PrintNumber { get; private init; }

    private CardInstance() { }

    public RosterId? RosterId { get; private set; }

    public static CardInstance Create(CardInstanceId id, CardId cardId, UserId ownerId, int printNumber, int printRun)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(printNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(printNumber, printRun);

        var instance = new CardInstance { Id = id, CardId = cardId, OwnerId = ownerId, PrintNumber = printNumber };
        instance.Raise(new CardInstanceCreated(id, cardId, ownerId));
        return instance;
    }

    public void AddToRoster(RosterId rosterId)
    {
        RosterId = rosterId;
        Raise(new CardInstanceAddedToRoster(Id, rosterId));
    }

    public void RemoveFromRoster(RosterId rosterId)
    {
        RosterId = null;
        Raise(new CardInstanceRemovedFromRoster(Id, rosterId));
    }

    public void TransferOwnership(UserId newOwner)
    {
        var fromOwner = OwnerId;
        // Evict from roster before changing owner so the roster tuple can be cleaned up
        if (RosterId is { } rosterId)
        {
            RosterId = null;
            Raise(new CardInstanceRemovedFromRoster(Id, rosterId));
        }
        OwnerId = newOwner;
        Raise(new CardInstanceOwnershipTransferred(Id, fromOwner, newOwner));
    }
}
