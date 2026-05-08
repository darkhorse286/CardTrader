using CardTrader.Domain.Common;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Events;

public sealed record CardInstanceCreated(
    CardInstanceId Id, CardId CardId, UserId OwnerId) : IDomainEvent;

public sealed record CardInstanceAddedToCollection(
    CardInstanceId Id, CollectionId CollectionId) : IDomainEvent;

public sealed record CardInstanceRemovedFromCollection(
    CardInstanceId Id, CollectionId CollectionId) : IDomainEvent;
