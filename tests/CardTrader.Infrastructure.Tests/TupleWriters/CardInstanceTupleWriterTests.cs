using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.OpenFga;
using CardTrader.Infrastructure.TupleWriters;
using NSubstitute;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.Tests.TupleWriters;

public sealed class CardInstanceTupleWriterTests
{
    private readonly IFgaWriteClient _fga = Substitute.For<IFgaWriteClient>();
    private readonly CardInstanceTupleWriter _sut;

    public CardInstanceTupleWriterTests() => _sut = new CardInstanceTupleWriter(_fga);

    // ── CanHandle ────────────────────────────────────────────────────────────

    [Fact]
    public void CanHandle_CardInstanceCreated_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new CardInstanceCreated(CardInstanceId.New(), CardId.New(), UserId.New())));

    [Fact]
    public void CanHandle_CardInstanceAddedToRoster_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new CardInstanceAddedToRoster(CardInstanceId.New(), RosterId.New())));

    [Fact]
    public void CanHandle_CardInstanceRemovedFromRoster_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new CardInstanceRemovedFromRoster(CardInstanceId.New(), RosterId.New())));

    [Fact]
    public void CanHandle_UnrelatedEvent_ReturnsFalse()
        => Assert.False(_sut.CanHandle(new RosterCreated(RosterId.New(), "x", UserId.New())));

    // ── CardInstanceCreated ──────────────────────────────────────────────────

    [Fact]
    public async Task CardInstanceCreated_WritesOwnerTuple()
    {
        var instanceId = CardInstanceId.New();
        var ownerId = UserId.New();

        await _sut.HandleAsync(new CardInstanceCreated(instanceId, CardId.New(), ownerId));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{ownerId}" &&
                t[0].Relation == FgaRelations.Owner &&
                t[0].Object == $"{FgaTypes.CardInstance}:{instanceId}"),
            Arg.Any<CancellationToken>());
    }

    // ── CardInstanceAddedToRoster ────────────────────────────────────────────

    [Fact]
    public async Task CardInstanceAddedToRoster_WritesRosterTuple()
    {
        var instanceId = CardInstanceId.New();
        var rosterId = RosterId.New();

        await _sut.HandleAsync(new CardInstanceAddedToRoster(instanceId, rosterId));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"{FgaTypes.Roster}:{rosterId}" &&
                t[0].Relation == FgaRelations.Roster &&
                t[0].Object == $"{FgaTypes.CardInstance}:{instanceId}"),
            Arg.Any<CancellationToken>());
    }

    // ── CardInstanceRemovedFromRoster ────────────────────────────────────────

    [Fact]
    public async Task CardInstanceRemovedFromRoster_DeletesRosterTuple()
    {
        var instanceId = CardInstanceId.New();
        var rosterId = RosterId.New();

        await _sut.HandleAsync(new CardInstanceRemovedFromRoster(instanceId, rosterId));

        await _fga.Received(1).DeleteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKeyWithoutCondition>>(t =>
                t.Count == 1 &&
                t[0].User == $"{FgaTypes.Roster}:{rosterId}" &&
                t[0].Relation == FgaRelations.Roster &&
                t[0].Object == $"{FgaTypes.CardInstance}:{instanceId}"),
            Arg.Any<CancellationToken>());
    }

    // ── CardInstanceOwnershipTransferred ─────────────────────────────────────

    [Fact]
    public void CanHandle_CardInstanceOwnershipTransferred_ReturnsTrue()
        => Assert.True(_sut.CanHandle(
            new CardInstanceOwnershipTransferred(CardInstanceId.New(), UserId.New(), UserId.New())));

    [Fact]
    public async Task CardInstanceOwnershipTransferred_DeletesOldTupleAndWritesNew()
    {
        var instanceId = CardInstanceId.New();
        var fromOwner  = UserId.New();
        var toOwner    = UserId.New();
        var obj        = $"{FgaTypes.CardInstance}:{instanceId}";

        await _sut.HandleAsync(new CardInstanceOwnershipTransferred(instanceId, fromOwner, toOwner));

        await _fga.Received(1).DeleteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKeyWithoutCondition>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{fromOwner}" &&
                t[0].Relation == FgaRelations.Owner &&
                t[0].Object == obj),
            Arg.Any<CancellationToken>());

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{toOwner}" &&
                t[0].Relation == FgaRelations.Owner &&
                t[0].Object == obj),
            Arg.Any<CancellationToken>());
    }
}
