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
        await GuardPrintNumberUniqueAsync(cardId, printNumber, ct);
        var instance = CardInstance.Create(id, cardId, ownerId, printNumber);
        await instances.AddAsync(instance, ct);
        await dispatcher.DispatchAsync(instance.DomainEvents, ct);
        instance.ClearDomainEvents();
        return instance;
    }

    public async Task<CardInstance> MintAndAddToRosterAsync(
        CardInstanceId id, CardId cardId, UserId ownerId, int printNumber, RosterId rosterId,
        CancellationToken ct = default)
    {
        await GuardPrintNumberUniqueAsync(cardId, printNumber, ct);
        await ValidateRosterSlotAsync(cardId, rosterId, ct);

        var instance = CardInstance.Create(id, cardId, ownerId, printNumber);
        instance.AddToRoster(rosterId);
        await instances.AddAsync(instance, ct);
        await dispatcher.DispatchAsync(instance.DomainEvents, ct);
        instance.ClearDomainEvents();
        return instance;
    }

    public Task<IReadOnlyList<CardInstance>> GetByRosterAsync(
        RosterId rosterId, CancellationToken ct = default)
        => instances.GetByRosterAsync(rosterId, ct);

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

    private async Task GuardPrintNumberUniqueAsync(CardId cardId, int printNumber, CancellationToken ct)
    {
        if (await instances.ExistsByCardAndPrintNumberAsync(cardId, printNumber, ct))
            throw new InvalidOperationException(
                $"Print #{printNumber} for card {cardId} already exists.");
    }

    private async Task ValidateRosterSlotAsync(CardId cardId, RosterId rosterId, CancellationToken ct)
    {
        var existing = await instances.GetByRosterAsync(rosterId, ct);

        if (existing.Count >= RosterConstraints.MaxSize)
            throw new InvalidOperationException(
                $"Roster is full — maximum {RosterConstraints.MaxSize} players allowed.");

        var card = await cards.GetByIdAsync(cardId, ct);
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
