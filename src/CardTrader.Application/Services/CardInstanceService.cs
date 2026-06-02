using CardTrader.Application.Abstractions;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Services;

public sealed class CardInstanceService(
    ICardInstanceRepository instances,
    ICardRepository cards,
    IAuthorizationService authz,
    IDomainEventDispatcher dispatcher)
{
    public async Task<CardInstance> CreateAsync(
        CardInstanceId id, CardId cardId, UserId ownerId, int printNumber, CancellationToken ct = default)
    {
        var card = await GetCardOrThrowAsync(cardId, ct);
        await GuardPrintNumberUniqueAsync(cardId, printNumber, ct);
        var instance = CardInstance.Create(id, cardId, ownerId, printNumber, card.PrintRun);
        await instances.AddAsync(instance, ct);
        await dispatcher.DispatchAsync(instance.DomainEvents, ct);
        instance.ClearDomainEvents();
        return instance;
    }

    public async Task<CardInstance> MintAndAddToRosterAsync(
        CardInstanceId id, CardId cardId, UserId ownerId, int printNumber, RosterId rosterId,
        CancellationToken ct = default)
    {
        var card = await GetCardOrThrowAsync(cardId, ct);
        await GuardPrintNumberUniqueAsync(cardId, printNumber, ct);
        await ValidateRosterSlotAsync(card, rosterId, ct);

        var instance = CardInstance.Create(id, cardId, ownerId, printNumber, card.PrintRun);
        instance.AddToRoster(rosterId);
        await instances.AddAsync(instance, ct);
        await dispatcher.DispatchAsync(instance.DomainEvents, ct);
        instance.ClearDomainEvents();
        return instance;
    }

    public Task<IReadOnlyList<CardInstance>> GetByRosterAsync(
        RosterId rosterId, CancellationToken ct = default)
        => instances.GetByRosterAsync(rosterId, ct);

    public Task<IReadOnlyList<CardInstance>> GetOwnedAsync(
        UserId ownerId, CancellationToken ct = default)
        => instances.GetByOwnerAsync(ownerId, ct);

    public async Task AddToRosterAsync(
        CardInstanceId id, UserId requestingUserId, RosterId rosterId, CancellationToken ct = default)
    {
        var instance = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        instance.AddToRoster(rosterId);
        await instances.UpdateAsync(instance, ct);
        await dispatcher.DispatchAsync(instance.DomainEvents, ct);
        instance.ClearDomainEvents();
    }

    public async Task RemoveFromRosterAsync(
        CardInstanceId id, UserId requestingUserId, RosterId rosterId, CancellationToken ct = default)
    {
        var instance = await GetOrThrowAsync(id, ct);
        await CheckOrThrowAsync(requestingUserId, FgaRelations.CanManage, id, ct);
        instance.RemoveFromRoster(rosterId);
        await instances.UpdateAsync(instance, ct);
        await dispatcher.DispatchAsync(instance.DomainEvents, ct);
        instance.ClearDomainEvents();
    }

    private async Task<Card> GetCardOrThrowAsync(CardId cardId, CancellationToken ct)
        => await cards.GetByIdAsync(cardId, ct)
           ?? throw new KeyNotFoundException($"Card {cardId} not found.");

    private async Task GuardPrintNumberUniqueAsync(CardId cardId, int printNumber, CancellationToken ct)
    {
        if (await instances.ExistsByCardAndPrintNumberAsync(cardId, printNumber, ct))
            throw new InvalidOperationException(
                $"Print #{printNumber} is already taken for this card.");
    }

    private async Task ValidateRosterSlotAsync(Card? card, RosterId rosterId, CancellationToken ct)
    {
        var existing = await instances.GetByRosterAsync(rosterId, ct);

        if (existing.Count >= RosterConstraints.MaxSize)
            throw new InvalidOperationException(
                $"Roster is full — maximum {RosterConstraints.MaxSize} players allowed.");

        if (card?.PlayerPosition is not { } position)
            return;

        if (!RosterConstraints.PositionSlots.TryGetValue(position, out var maxSlots))
            return;

        int posCount = 0;
        foreach (var inst in existing)
        {
            var instCard = await cards.GetByIdAsync(inst.CardId, ct);
            if (instCard?.PlayerPosition == position)
                posCount++;
        }

        if (posCount >= maxSlots)
            throw new InvalidOperationException(
                $"Roster already has {maxSlots} {position} player(s) — position slot is full.");
    }

    private async Task<CardInstance> GetOrThrowAsync(CardInstanceId id, CancellationToken ct)
        => await instances.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"CardInstance {id} not found.");

    private Task CheckOrThrowAsync(UserId userId, string relation, CardInstanceId objectId, CancellationToken ct)
        => CheckOrThrowAsync(
            $"{FgaTypes.User}:{userId}", relation, $"{FgaTypes.CardInstance}:{objectId}", ct);

    private async Task CheckOrThrowAsync(string user, string relation, string @object, CancellationToken ct)
    {
        if (!await authz.CheckAsync(user, relation, @object, ct))
            throw new UnauthorizedAccessException();
    }
}
