using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Tests.Entities;

public sealed class CardTests
{
    private static readonly CardId Id = CardId.New();

    private static Card MakeCard(
        string name = "Black Lotus",
        string setName = "Alpha",
        string rarity = "Rare",
        string playerName = "Garfield",
        int printRun = 100,
        string? playerPosition = null,
        string? teamName = null,
        string? imageUrl = null)
        => Card.Create(Id, name, setName, rarity, playerName, printRun, playerPosition, teamName, imageUrl);

    // ── Create: happy path ───────────────────────────────────────────────────

    [Fact]
    public void Create_SetsAllProperties()
    {
        var card = Card.Create(
            Id, "Black Lotus", "Alpha", "Rare",
            playerName: "Garfield",
            printRun: 10,
            playerPosition: "Midfield",
            teamName: "Arsenal",
            imageUrl: "https://example.com/img.png");

        Assert.Equal(Id, card.Id);
        Assert.Equal("Black Lotus", card.Name);
        Assert.Equal("Alpha", card.SetName);
        Assert.Equal("Rare", card.Rarity);
        Assert.Equal("Garfield", card.PlayerName);
        Assert.Equal(10, card.PrintRun);
        Assert.Equal("Midfield", card.PlayerPosition);
        Assert.Equal("Arsenal", card.TeamName);
        Assert.Equal("https://example.com/img.png", card.ImageUrl);
    }

    [Fact]
    public void Create_OptionalFieldsDefault_ToNull()
    {
        var card = MakeCard();

        Assert.Null(card.PlayerPosition);
        Assert.Null(card.TeamName);
        Assert.Null(card.ImageUrl);
    }

    [Fact]
    public void Create_RaisesNoDomainEvents()
    {
        // Card is a catalog archetype — no FGA ownership tuples are written.
        var card = MakeCard();

        Assert.Empty(card.DomainEvents);
    }

    // ── Create: validation ───────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string name)
        => Assert.Throws<ArgumentException>(() => MakeCard(name: name));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptySetName_Throws(string setName)
        => Assert.Throws<ArgumentException>(() => MakeCard(setName: setName));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyRarity_Throws(string rarity)
        => Assert.Throws<ArgumentException>(() => MakeCard(rarity: rarity));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyPlayerName_Throws(string playerName)
        => Assert.Throws<ArgumentException>(() => MakeCard(playerName: playerName));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_PrintRunLessThanOne_Throws(int printRun)
        => Assert.Throws<ArgumentOutOfRangeException>(() => MakeCard(printRun: printRun));
}
