using CardTrader.Domain.Entities;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Tests.Entities;

public sealed class CollectionTests
{
    private static readonly CollectionId Id = CollectionId.New();
    private static readonly UserId OwnerId = UserId.New();
    private static readonly UserId OtherUser = UserId.New();

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsProperties()
    {
        var coll = Collection.Create(Id, "My Collection", OwnerId);

        Assert.Equal(Id, coll.Id);
        Assert.Equal("My Collection", coll.Name);
        Assert.Equal(OwnerId, coll.OwnerId);
    }

    [Fact]
    public void Create_RaisesCollectionCreated()
    {
        var coll = Collection.Create(Id, "My Collection", OwnerId);

        var evt = Assert.Single(coll.DomainEvents);
        var created = Assert.IsType<CollectionCreated>(evt);
        Assert.Equal(Id, created.Id);
        Assert.Equal("My Collection", created.Name);
        Assert.Equal(OwnerId, created.OwnerId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string name)
        => Assert.Throws<ArgumentException>(() => Collection.Create(Id, name, OwnerId));

    // ── ShareWithUser ────────────────────────────────────────────────────────

    [Fact]
    public void ShareWithUser_RaisesCollectionSharedWithUser()
    {
        var coll = Collection.Create(Id, "My Collection", OwnerId);
        coll.ClearDomainEvents();

        coll.ShareWithUser(OtherUser);

        var evt = Assert.Single(coll.DomainEvents);
        var shared = Assert.IsType<CollectionSharedWithUser>(evt);
        Assert.Equal(Id, shared.Id);
        Assert.Equal(OtherUser, shared.SharedWithUserId);
    }

    // ── Unshare ──────────────────────────────────────────────────────────────

    [Fact]
    public void Unshare_RaisesCollectionUnshared()
    {
        var coll = Collection.Create(Id, "My Collection", OwnerId);
        coll.ClearDomainEvents();

        coll.Unshare(OtherUser);

        var evt = Assert.Single(coll.DomainEvents);
        var unshared = Assert.IsType<CollectionUnshared>(evt);
        Assert.Equal(Id, unshared.Id);
        Assert.Equal(OtherUser, unshared.UnsharedFromUserId);
    }

    // ── MakePublic / MakePrivate ─────────────────────────────────────────────

    [Fact]
    public void MakePublic_RaisesCollectionMadePublic()
    {
        var coll = Collection.Create(Id, "My Collection", OwnerId);
        coll.ClearDomainEvents();

        coll.MakePublic();

        var evt = Assert.Single(coll.DomainEvents);
        var pub = Assert.IsType<CollectionMadePublic>(evt);
        Assert.Equal(Id, pub.Id);
    }

    [Fact]
    public void MakePrivate_RaisesCollectionMadePrivate()
    {
        var coll = Collection.Create(Id, "My Collection", OwnerId);
        coll.ClearDomainEvents();

        coll.MakePrivate();

        var evt = Assert.Single(coll.DomainEvents);
        var priv = Assert.IsType<CollectionMadePrivate>(evt);
        Assert.Equal(Id, priv.Id);
    }

    // ── Event accumulation ───────────────────────────────────────────────────

    [Fact]
    public void MultipleOperations_EventsAccumulateInOrder()
    {
        var coll = Collection.Create(Id, "My Collection", OwnerId);
        coll.ClearDomainEvents();

        coll.MakePublic();
        coll.ShareWithUser(OtherUser);
        coll.MakePrivate();

        Assert.Equal(3, coll.DomainEvents.Count);
        Assert.IsType<CollectionMadePublic>(coll.DomainEvents[0]);
        Assert.IsType<CollectionSharedWithUser>(coll.DomainEvents[1]);
        Assert.IsType<CollectionMadePrivate>(coll.DomainEvents[2]);
    }
}
