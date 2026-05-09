using CardTrader.Domain.Entities;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Tests.Entities;

public sealed class CardInstanceTests
{
    private static readonly CardInstanceId InstanceId = CardInstanceId.New();
    private static readonly CardId CardId = CardId.New();
    private static readonly UserId OwnerId = UserId.New();
    private static readonly RosterId RosterId = RosterId.New();

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsProperties()
    {
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId, printNumber: 42);

        Assert.Equal(InstanceId, instance.Id);
        Assert.Equal(CardId, instance.CardId);
        Assert.Equal(OwnerId, instance.OwnerId);
        Assert.Equal(42, instance.PrintNumber);
    }

    [Fact]
    public void Create_RaisesCardInstanceCreated()
    {
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId, printNumber: 1);

        var evt = Assert.Single(instance.DomainEvents);
        var created = Assert.IsType<CardInstanceCreated>(evt);
        Assert.Equal(InstanceId, created.Id);
        Assert.Equal(CardId, created.CardId);
        Assert.Equal(OwnerId, created.OwnerId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_PrintNumberLessThanOne_Throws(int printNumber)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => CardInstance.Create(InstanceId, CardId, OwnerId, printNumber));

    // ── AddToRoster ──────────────────────────────────────────────────────────

    [Fact]
    public void AddToRoster_RaisesCardInstanceAddedToRoster()
    {
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId, printNumber: 1);
        instance.ClearDomainEvents();

        instance.AddToRoster(RosterId);

        var evt = Assert.Single(instance.DomainEvents);
        var added = Assert.IsType<CardInstanceAddedToRoster>(evt);
        Assert.Equal(InstanceId, added.Id);
        Assert.Equal(RosterId, added.RosterId);
    }

    // ── RemoveFromRoster ─────────────────────────────────────────────────────

    [Fact]
    public void RemoveFromRoster_RaisesCardInstanceRemovedFromRoster()
    {
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId, printNumber: 1);
        instance.ClearDomainEvents();

        instance.RemoveFromRoster(RosterId);

        var evt = Assert.Single(instance.DomainEvents);
        var removed = Assert.IsType<CardInstanceRemovedFromRoster>(evt);
        Assert.Equal(InstanceId, removed.Id);
        Assert.Equal(RosterId, removed.RosterId);
    }

    // ── Event accumulation ───────────────────────────────────────────────────

    [Fact]
    public void MultipleOperations_EventsAccumulateInOrder()
    {
        var rosterA = RosterId.New();
        var rosterB = RosterId.New();
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId, printNumber: 1);
        instance.ClearDomainEvents();

        instance.AddToRoster(rosterA);
        instance.AddToRoster(rosterB);
        instance.RemoveFromRoster(rosterA);

        Assert.Equal(3, instance.DomainEvents.Count);
        Assert.IsType<CardInstanceAddedToRoster>(instance.DomainEvents[0]);
        Assert.IsType<CardInstanceAddedToRoster>(instance.DomainEvents[1]);
        Assert.IsType<CardInstanceRemovedFromRoster>(instance.DomainEvents[2]);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesList()
    {
        var instance = CardInstance.Create(InstanceId, CardId, OwnerId, printNumber: 1);
        Assert.NotEmpty(instance.DomainEvents);

        instance.ClearDomainEvents();

        Assert.Empty(instance.DomainEvents);
    }
}
