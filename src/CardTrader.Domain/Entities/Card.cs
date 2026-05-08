using CardTrader.Domain.Common;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Entities;

// Card is the archetype (catalog entry). Ownership tuples are never written for archetypes —
// they attach to CardInstance only. No domain events are raised on Card creation.
public sealed class Card : Entity<CardId>
{
    public string Name { get; private init; } = "";
    public string SetName { get; private init; } = "";
    public string Rarity { get; private init; } = "";
    public string PlayerName { get; private init; } = "";
    public string? PlayerPosition { get; private init; }
    public string? TeamName { get; private init; }
    public string? ImageUrl { get; private init; }
    public int PrintRun { get; private init; }

    private Card() { }

    public static Card Create(
        CardId id,
        string name,
        string setName,
        string rarity,
        string playerName,
        int printRun,
        string? playerPosition = null,
        string? teamName = null,
        string? imageUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(setName);
        ArgumentException.ThrowIfNullOrWhiteSpace(rarity);
        ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
        ArgumentOutOfRangeException.ThrowIfLessThan(printRun, 1);

        return new Card
        {
            Id = id,
            Name = name,
            SetName = setName,
            Rarity = rarity,
            PlayerName = playerName,
            PrintRun = printRun,
            PlayerPosition = playerPosition,
            TeamName = teamName,
            ImageUrl = imageUrl,
        };
    }
}
