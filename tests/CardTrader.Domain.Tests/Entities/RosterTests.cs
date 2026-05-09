using CardTrader.Domain.Entities;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Tests.Entities;

public sealed class RosterTests
{
    private static readonly RosterId Id = RosterId.New();
    private static readonly UserId OwnerId = UserId.New();
    private static readonly UserId OtherUser = UserId.New();

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsProperties()
    {
        var roster = Roster.Create(Id, "My Roster", OwnerId);

        Assert.Equal(Id, roster.Id);
        Assert.Equal("My Roster", roster.Name);
        Assert.Equal(OwnerId, roster.OwnerId);
    }

    [Fact]
    public void Create_RaisesRosterCreated()
    {
        var roster = Roster.Create(Id, "My Roster", OwnerId);

        var evt = Assert.Single(roster.DomainEvents);
        var created = Assert.IsType<RosterCreated>(evt);
        Assert.Equal(Id, created.Id);
        Assert.Equal("My Roster", created.Name);
        Assert.Equal(OwnerId, created.OwnerId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string name)
        => Assert.Throws<ArgumentException>(() => Roster.Create(Id, name, OwnerId));

    // ── ShareWithUser ────────────────────────────────────────────────────────

    [Fact]
    public void ShareWithUser_RaisesRosterSharedWithUser()
    {
        var roster = Roster.Create(Id, "My Roster", OwnerId);
        roster.ClearDomainEvents();

        roster.ShareWithUser(OtherUser);

        var evt = Assert.Single(roster.DomainEvents);
        var shared = Assert.IsType<RosterSharedWithUser>(evt);
        Assert.Equal(Id, shared.Id);
        Assert.Equal(OtherUser, shared.SharedWithUserId);
    }

    // ── Unshare ──────────────────────────────────────────────────────────────

    [Fact]
    public void Unshare_RaisesRosterUnshared()
    {
        var roster = Roster.Create(Id, "My Roster", OwnerId);
        roster.ClearDomainEvents();

        roster.Unshare(OtherUser);

        var evt = Assert.Single(roster.DomainEvents);
        var unshared = Assert.IsType<RosterUnshared>(evt);
        Assert.Equal(Id, unshared.Id);
        Assert.Equal(OtherUser, unshared.UnsharedFromUserId);
    }

    // ── MakePublic / MakePrivate ─────────────────────────────────────────────

    [Fact]
    public void MakePublic_RaisesRosterMadePublic()
    {
        var roster = Roster.Create(Id, "My Roster", OwnerId);
        roster.ClearDomainEvents();

        roster.MakePublic();

        var evt = Assert.Single(roster.DomainEvents);
        var pub = Assert.IsType<RosterMadePublic>(evt);
        Assert.Equal(Id, pub.Id);
    }

    [Fact]
    public void MakePrivate_RaisesRosterMadePrivate()
    {
        var roster = Roster.Create(Id, "My Roster", OwnerId);
        roster.ClearDomainEvents();

        roster.MakePrivate();

        var evt = Assert.Single(roster.DomainEvents);
        var priv = Assert.IsType<RosterMadePrivate>(evt);
        Assert.Equal(Id, priv.Id);
    }

    // ── Event accumulation ───────────────────────────────────────────────────

    [Fact]
    public void MultipleOperations_EventsAccumulateInOrder()
    {
        var roster = Roster.Create(Id, "My Roster", OwnerId);
        roster.ClearDomainEvents();

        roster.MakePublic();
        roster.ShareWithUser(OtherUser);
        roster.MakePrivate();

        Assert.Equal(3, roster.DomainEvents.Count);
        Assert.IsType<RosterMadePublic>(roster.DomainEvents[0]);
        Assert.IsType<RosterSharedWithUser>(roster.DomainEvents[1]);
        Assert.IsType<RosterMadePrivate>(roster.DomainEvents[2]);
    }
}
