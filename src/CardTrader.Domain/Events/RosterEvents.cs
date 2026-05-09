using CardTrader.Domain.Common;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Events;

public sealed record RosterCreated(
    RosterId Id, string Name, UserId OwnerId) : IDomainEvent;

public sealed record RosterSharedWithUser(
    RosterId Id, UserId SharedWithUserId) : IDomainEvent;

// Removes the specific user's viewer tuple.
public sealed record RosterUnshared(
    RosterId Id, UserId UnsharedFromUserId) : IDomainEvent;

// Writes user:* viewer tuple — makes the roster publicly visible.
public sealed record RosterMadePublic(RosterId Id) : IDomainEvent;

// Deletes user:* viewer tuple — returns the roster to private.
public sealed record RosterMadePrivate(RosterId Id) : IDomainEvent;
