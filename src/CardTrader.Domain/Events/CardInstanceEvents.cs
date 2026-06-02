using CardTrader.Domain.Common;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Events;

public sealed record CardInstanceCreated(
    CardInstanceId Id, CardId CardId, UserId OwnerId) : IDomainEvent;

public sealed record CardInstanceAddedToRoster(
    CardInstanceId Id, RosterId RosterId) : IDomainEvent;

public sealed record CardInstanceRemovedFromRoster(
    CardInstanceId Id, RosterId RosterId) : IDomainEvent;

public sealed record CardInstanceOwnershipTransferred(
    CardInstanceId Id, UserId FromOwner, UserId ToOwner) : IDomainEvent;
