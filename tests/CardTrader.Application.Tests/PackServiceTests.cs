using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Domain.Common;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using NSubstitute;

namespace CardTrader.Application.Tests;

public class PackServiceTests
{
    private readonly ICardRepository _cards = Substitute.For<ICardRepository>();
    private readonly ICardInstanceRepository _instances = Substitute.For<ICardInstanceRepository>();
    private readonly IPackClaimRepository _packClaims = Substitute.For<IPackClaimRepository>();
    private readonly IAuthorizationService _authz = Substitute.For<IAuthorizationService>();
    private readonly IDomainEventDispatcher _dispatcher = Substitute.For<IDomainEventDispatcher>();

    private static readonly DateTimeOffset Now = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    // TimeProvider that returns a fixed instant
    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    // Random that always returns `value % maxValue` — fully deterministic
    private sealed class FixedRandom(int value) : Random
    {
        public override int Next(int maxValue) => maxValue <= 0 ? 0 : value % maxValue;
        public override int Next(int minValue, int maxValue) =>
            maxValue <= minValue ? minValue : minValue + Next(maxValue - minValue);
        public override int Next() => value;
    }

    // FixedRandom(0)  → Next(10) == 0 → God Pack
    // FixedRandom(1)  → Next(10) == 1 → Normal pack
    private static readonly Random GodRng    = new FixedRandom(0);
    private static readonly Random NormalRng = new FixedRandom(1);

    private static Card MakeCard(string rarity, int printRun = 200) =>
        Card.Create(CardId.New(), $"{rarity} Card", "Test Set", rarity, "Test Player", printRun,
            playerPosition: "Attack");

    private readonly List<Card> _catalogue;

    private CardInstanceService MakeCardInstanceSvc() =>
        new(_instances, _cards, _authz, _dispatcher);

    private PackService MakeSut(TimeProvider? time = null, Random? rng = null) =>
        new(_cards, _instances, _packClaims, MakeCardInstanceSvc(),
            time ?? new FakeTimeProvider(Now), rng ?? NormalRng);

    public PackServiceTests()
    {
        _catalogue =
        [
            MakeCard("Legendary"),
            MakeCard("Ultra Rare"),
            MakeCard("Rare"),
            MakeCard("Uncommon"),
            MakeCard("Common"),
        ];

        _cards.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Card>)_catalogue);
        _cards.GetByIdAsync(Arg.Any<CardId>(), Arg.Any<CancellationToken>())
            .Returns(ci => _catalogue.FirstOrDefault(c => c.Id == ci.Arg<CardId>()));

        _packClaims.GetLastClaimAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((DateTimeOffset?)null);
        _instances.ExistsByCardAndPrintNumberAsync(Arg.Any<CardId>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _instances.GetTakenPrintNumbersAsync(Arg.Any<CardId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<int>)[]);
        _instances.GetByRosterAsync(Arg.Any<RosterId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CardInstance>)[]);
    }

    // ── Pack size & structure ────────────────────────────────────────────────

    [Fact]
    public async Task ClaimPack_NoPriorClaim_Returns10Cards()
    {
        var result = await MakeSut().ClaimPackAsync(UserId.New());
        Assert.Equal(10, result.Cards.Count);
    }

    [Fact]
    public async Task ClaimPack_NormalPack_HasCorrectRaritySlots()
    {
        var result = await MakeSut(rng: NormalRng).ClaimPackAsync(UserId.New());

        Assert.False(result.IsGodPack);
        var rarities = result.Cards.Select(c => c.Card.Rarity).ToList();
        Assert.Equal(1, rarities.Count(r => r is "Legendary" or "Ultra Rare" or "Rare"));
        Assert.Equal(4, rarities.Count(r => r == "Uncommon"));
        Assert.Equal(5, rarities.Count(r => r == "Common"));
    }

    [Fact]
    public async Task ClaimPack_GodPack_AllLegendary()
    {
        var result = await MakeSut(rng: GodRng).ClaimPackAsync(UserId.New());

        Assert.True(result.IsGodPack);
        Assert.Equal(10, result.Cards.Count);
        Assert.All(result.Cards, c => Assert.Equal("Legendary", c.Card.Rarity));
    }

    [Fact]
    public async Task ClaimPack_AllInstancesHaveUniqueCardAndPrintNumberPairs()
    {
        var result = await MakeSut(rng: GodRng).ClaimPackAsync(UserId.New());

        var pairs = result.Cards.Select(c => (c.Card.Id, c.Instance.PrintNumber)).ToList();
        Assert.Equal(pairs.Count, pairs.Distinct().Count());
    }

    // ── Time gate ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClaimPack_AlreadyClaimedToday_Throws()
    {
        _packClaims.GetLastClaimAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Now.AddHours(-3)); // same UTC day

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => MakeSut().ClaimPackAsync(UserId.New()));
    }

    [Fact]
    public async Task ClaimPack_ClaimedYesterday_Succeeds()
    {
        _packClaims.GetLastClaimAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Now.AddDays(-1));

        var result = await MakeSut().ClaimPackAsync(UserId.New());
        Assert.Equal(10, result.Cards.Count);
    }

    [Fact]
    public async Task ClaimPack_RecordsClaimTimestamp()
    {
        await MakeSut().ClaimPackAsync(UserId.New());

        await _packClaims.Received(1)
            .SetLastClaimAsync(Arg.Any<UserId>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClaimPack_BlockedByGate_DoesNotRecordClaim()
    {
        _packClaims.GetLastClaimAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Now.AddHours(-1));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => MakeSut().ClaimPackAsync(UserId.New()));

        await _packClaims.DidNotReceive()
            .SetLastClaimAsync(Arg.Any<UserId>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
