using CardTrader.Domain.Entities;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Tests.Entities;

public sealed class CardInstanceTests
{
    private static readonly CardInstanceId InstanceId = CardInstanceId.New();
    private static readonly CardId CardId = CardId.New();
    private static readonly UserId OwnerId = UserId.New();
    private static readonly CollectionId CollectionId = CollectionId.New();

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsProperties()
    {
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId);

        Assert.Equal(InstanceId, instance.Id);
        Assert.Equal(CardId, instance.CardId);
        Assert.Equal(OwnerId, instance.OwnerId);
    }

    [Fact]
    public void Create_RaisesCardInstanceCreated()
    {
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId);

        var evt = Assert.Single(instance.DomainEvents);
        var created = Assert.IsType<CardInstanceCreated>(evt);
        Assert.Equal(InstanceId, created.Id);
        Assert.Equal(CardId, created.CardId);
        Assert.Equal(OwnerId, created.OwnerId);
    }

    // ── AddToCollection ──────────────────────────────────────────────────────

    [Fact]
    public void AddToCollection_RaisesCardInstanceAddedToCollection()
    {
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId);
        instance.ClearDomainEvents();

        instance.AddToCollection(CollectionId);

        var evt = Assert.Single(instance.DomainEvents);
        var added = Assert.IsType<CardInstanceAddedToCollection>(evt);
        Assert.Equal(InstanceId, added.Id);
        Assert.Equal(CollectionId, added.CollectionId);
    }

    // ── RemoveFromCollection ─────────────────────────────────────────────────

    [Fact]
    public void RemoveFromCollection_RaisesCardInstanceRemovedFromCollection()
    {
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId);
        instance.ClearDomainEvents();

        instance.RemoveFromCollection(CollectionId);

        var evt = Assert.Single(instance.DomainEvents);
        var removed = Assert.IsType<CardInstanceRemovedFromCollection>(evt);
        Assert.Equal(InstanceId, removed.Id);
        Assert.Equal(CollectionId, removed.CollectionId);
    }

    // ── Event accumulation ───────────────────────────────────────────────────

    [Fact]
    public void MultipleOperations_EventsAccumulateInOrder()
    {
        var collA = CollectionId.New();
        var collB = CollectionId.New();
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId);
        instance.ClearDomainEvents();

        instance.AddToCollection(collA);
        instance.AddToCollection(collB);
        instance.RemoveFromCollection(collA);

        Assert.Equal(3, instance.DomainEvents.Count);
        Assert.IsType<CardInstanceAddedToCollection>(instance.DomainEvents[0]);
        Assert.IsType<CardInstanceAddedToCollection>(instance.DomainEvents[1]);
        Assert.IsType<CardInstanceRemovedFromCollection>(instance.DomainEvents[2]);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesList()
    {
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId);
        Assert.NotEmpty(instance.DomainEvents);

        instance.ClearDomainEvents();

        Assert.Empty(instance.DomainEvents);
    }
}
