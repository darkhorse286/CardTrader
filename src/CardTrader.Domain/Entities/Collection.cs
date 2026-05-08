using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Entities;

public sealed class Collection : Entity<CollectionId>
{
    public string Name { get; private init; } = "";
    public UserId OwnerId { get; private init; }

    private Collection() { }

    public static Collection Create(CollectionId id, string name, UserId ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var collection = new Collection { Id = id, Name = name, OwnerId = ownerId };
        collection.Raise(new CollectionCreated(id, name, ownerId));
        return collection;
    }

    public void ShareWithUser(UserId userId)
        => Raise(new CollectionSharedWithUser(Id, userId));

    public void Unshare(UserId userId)
        => Raise(new CollectionUnshared(Id, userId));

    public void MakePublic()
        => Raise(new CollectionMadePublic(Id));

    public void MakePrivate()
        => Raise(new CollectionMadePrivate(Id));
}
