using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Application.Services;

public sealed class PackService(
    ICardRepository cards,
    ICardInstanceRepository instances,
    IPackClaimRepository packClaims,
    CardInstanceService cardInstanceSvc,
    TimeProvider time,
    Random rng)
{
    // Pack slot counts
    private const int PackSize      = 10;
    private const int RarePlusSlots = 1;
    private const int UncommonSlots = 4;
    private const int CommonSlots   = 5;
    private const int GodPackOdds   = 10; // 1 in 10

    private static readonly string[] RarePlus  = ["Legendary", "Ultra Rare", "Rare"];
    private static readonly string[] Uncommons  = ["Uncommon"];
    private static readonly string[] Commons    = ["Common"];
    private static readonly string[] Legendaries = ["Legendary"];

    public async Task<DateTimeOffset?> GetLastClaimAsync(UserId userId, CancellationToken ct = default)
        => await packClaims.GetLastClaimAsync(userId, ct);

    public async Task<PackResult> ClaimPackAsync(UserId userId, CancellationToken ct = default)
    {
        // Enforce once-per-day gate
        var lastClaim = await packClaims.GetLastClaimAsync(userId, ct);
        var today = time.GetUtcNow().UtcDateTime.Date;
        if (lastClaim?.UtcDateTime.Date >= today)
            throw new InvalidOperationException(
                "You've already claimed your pack today. Come back tomorrow!");

        var allCards = await cards.GetAllAsync(ct);

        // Roll for God Pack (1 in GodPackOdds)
        var isGodPack = rng.Next(GodPackOdds) == 0;

        // Track (CardId, PrintNumber) pairs reserved within this pack to avoid intra-pack duplicates
        var reserved = new HashSet<(CardId, int)>();

        List<(Card Card, int PrintNumber)> selected;
        if (isGodPack)
        {
            selected = await SelectSlotsAsync(allCards, Legendaries, PackSize, reserved, ct);
        }
        else
        {
            selected =
            [
                .. await SelectSlotsAsync(allCards, RarePlus,  RarePlusSlots, reserved, ct),
                .. await SelectSlotsAsync(allCards, Uncommons,  UncommonSlots, reserved, ct),
                .. await SelectSlotsAsync(allCards, Commons,    CommonSlots,   reserved, ct),
            ];
        }

        // Mint each selected (card, printNumber) pair
        var minted = new List<(Card, CardInstance)>(selected.Count);
        foreach (var (card, printNumber) in selected)
        {
            var instance = await cardInstanceSvc.CreateAsync(
                CardInstanceId.New(), card.Id, userId, printNumber, ct);
            minted.Add((card, instance));
        }

        await packClaims.SetLastClaimAsync(userId, time.GetUtcNow(), ct);

        return new PackResult(isGodPack, minted);
    }

    // Picks `count` (card, printNumber) pairs from cards matching `rarities`,
    // avoiding numbers already taken in the DB or reserved by earlier slots in this pack.
    private async Task<List<(Card, int)>> SelectSlotsAsync(
        IReadOnlyList<Card> allCards,
        string[] rarities,
        int count,
        HashSet<(CardId, int)> reserved,
        CancellationToken ct)
    {
        var eligible = allCards.Where(c => rarities.Contains(c.Rarity)).ToList();
        var result   = new List<(Card, int)>(count);
        // Cache DB-taken print numbers per card to avoid repeated queries
        var takenCache = new Dictionary<CardId, HashSet<int>>();

        for (int attempt = 0; attempt < count * 20 && result.Count < count; attempt++)
        {
            if (eligible.Count == 0) break;

            var card = eligible[rng.Next(eligible.Count)];

            if (!takenCache.TryGetValue(card.Id, out var takenSet))
            {
                var taken = await instances.GetTakenPrintNumbersAsync(card.Id, ct);
                takenSet = [.. taken];
                takenCache[card.Id] = takenSet;
            }

            var available = Enumerable.Range(1, card.PrintRun)
                .Where(n => !takenSet.Contains(n) && !reserved.Contains((card.Id, n)))
                .ToList();

            if (available.Count == 0)
            {
                // This card's print run is exhausted — remove from eligible pool
                eligible.Remove(card);
                takenCache.Remove(card.Id);
                continue;
            }

            var printNumber = available[rng.Next(available.Count)];
            reserved.Add((card.Id, printNumber));
            result.Add((card, printNumber));
        }

        return result;
    }
}
