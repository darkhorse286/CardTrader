using CardTrader.Domain.Common;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Entities;

public sealed class Roster : Entity<RosterId>
{
    public string Name { get; private init; } = "";
    public UserId OwnerId { get; private init; }

    private Roster() { }

    public static Roster Create(RosterId id, string name, UserId ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var roster = new Roster { Id = id, Name = name, OwnerId = ownerId };
        roster.Raise(new RosterCreated(id, name, ownerId));
        return roster;
    }

    public void ShareWithUser(UserId userId)
        => Raise(new RosterSharedWithUser(Id, userId));

    public void Unshare(UserId userId)
        => Raise(new RosterUnshared(Id, userId));

    public void MakePublic()
        => Raise(new RosterMadePublic(Id));

    public void MakePrivate()
        => Raise(new RosterMadePrivate(Id));
}
