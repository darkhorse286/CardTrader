using CardTrader.Application.Abstractions;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Services;

public sealed class CardService(ICardRepository cards, IAdminService admin)
{
    public Task<IReadOnlyList<Card>> GetAllAsync(CancellationToken ct = default)
        => cards.GetAllAsync(ct);

    public async Task<Card> AddAsync(
        CardId id,
        string name,
        string setName,
        string rarity,
        string playerName,
        int printRun,
        UserId requestingUserId,
        string? playerPosition = null,
        string? teamName = null,
        CancellationToken ct = default)
    {
        if (!await admin.IsAdminAsync(requestingUserId, ct))
            throw new UnauthorizedAccessException("Only admins can add cards to the catalogue.");

        var card = Card.Create(id, name, setName, rarity, playerName, printRun, playerPosition, teamName);
        await cards.AddAsync(card, ct);
        return card;
    }
}
