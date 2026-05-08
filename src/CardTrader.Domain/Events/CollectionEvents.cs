using CardTrader.Domain.Common;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Events;

public sealed record CollectionCreated(
    CollectionId Id, string Name, UserId OwnerId) : IDomainEvent;

public sealed record CollectionSharedWithUser(
    CollectionId Id, UserId SharedWithUserId) : IDomainEvent;

// Removes the specific user's viewer tuple.
public sealed record CollectionUnshared(
    CollectionId Id, UserId UnsharedFromUserId) : IDomainEvent;

// Writes user:* viewer tuple — makes the collection publicly visible.
public sealed record CollectionMadePublic(CollectionId Id) : IDomainEvent;

// Deletes user:* viewer tuple — returns the collection to private.
public sealed record CollectionMadePrivate(CollectionId Id) : IDomainEvent;
