using CardTrader.Domain.ValueObjects;

namespace CardTrader.Domain.Tests.ValueObjects;

public sealed class StronglyTypedIdTests
{
    // ── Value equality ───────────────────────────────────────────────────────

    [Fact]
    public void UserId_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(new UserId(guid), new UserId(guid));
    }

    [Fact]
    public void UserId_DifferentGuid_AreNotEqual()
        => Assert.NotEqual(UserId.New(), UserId.New());

    [Fact]
    public void CardId_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(new CardId(guid), new CardId(guid));
    }

    [Fact]
    public void CardInstanceId_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(new CardInstanceId(guid), new CardInstanceId(guid));
    }

    [Fact]
    public void CollectionId_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(new CollectionId(guid), new CollectionId(guid));
    }

    [Fact]
    public void TradeProposalId_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(new TradeProposalId(guid), new TradeProposalId(guid));
    }

    [Fact]
    public void DelegationId_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(new DelegationId(guid), new DelegationId(guid));
    }

    // ── New() generates unique values ────────────────────────────────────────

    [Fact] public void UserId_New_IsUnique()         => Assert.NotEqual(UserId.New(), UserId.New());
    [Fact] public void CardId_New_IsUnique()         => Assert.NotEqual(CardId.New(), CardId.New());
    [Fact] public void CardInstanceId_New_IsUnique() => Assert.NotEqual(CardInstanceId.New(), CardInstanceId.New());
    [Fact] public void CollectionId_New_IsUnique()   => Assert.NotEqual(CollectionId.New(), CollectionId.New());
    [Fact] public void TradeProposalId_New_IsUnique() => Assert.NotEqual(TradeProposalId.New(), TradeProposalId.New());
    [Fact] public void DelegationId_New_IsUnique()   => Assert.NotEqual(DelegationId.New(), DelegationId.New());

    // ── ToString ─────────────────────────────────────────────────────────────

    [Fact]
    public void UserId_ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(guid.ToString(), new UserId(guid).ToString());
    }

    [Fact]
    public void UserId_Parse_RoundTrips()
    {
        var original = UserId.New();
        var parsed = UserId.Parse(original.ToString());
        Assert.Equal(original, parsed);
    }

    // ── Cross-type non-equality (compile-time: different types can't be compared) ──

    [Fact]
    public void DifferentIdTypes_WithSameGuid_AreNotInterchangeable()
    {
        var guid = Guid.NewGuid();
        var userId = new UserId(guid);
        var cardId = new CardId(guid);

        // The two types share no common equality — this verifies distinct types exist.
        Assert.Equal(guid, userId.Value);
        Assert.Equal(guid, cardId.Value);
        Assert.NotEqual(userId.GetType(), cardId.GetType());
    }
}
