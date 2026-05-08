using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Tests.Entities;

public sealed class CardTests
{
    private static readonly CardId Id = CardId.New();

    // ── Create: happy path ───────────────────────────────────────────────────

    [Fact]
    public void Create_SetsAllProperties()
    {
        var card = Card.Create(Id, "Black Lotus", "Alpha", "Rare");

        Assert.Equal(Id, card.Id);
        Assert.Equal("Black Lotus", card.Name);
        Assert.Equal("Alpha", card.SetName);
        Assert.Equal("Rare", card.Rarity);
    }

    [Fact]
    public void Create_RaisesNoDomainEvents()
    {
        // Card is a catalog archetype — no FGA ownership tuples are written.
        var card = Card.Create(Id, "Black Lotus", "Alpha", "Rare");

        Assert.Empty(card.DomainEvents);
    }

    // ── Create: validation ───────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string name)
        => Assert.Throws<ArgumentException>(() => Card.Create(Id, name, "Alpha", "Rare"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptySetName_Throws(string setName)
        => Assert.Throws<ArgumentException>(() => Card.Create(Id, "Black Lotus", setName, "Rare"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyRarity_Throws(string rarity)
        => Assert.Throws<ArgumentException>(() => Card.Create(Id, "Black Lotus", "Alpha", rarity));
}
