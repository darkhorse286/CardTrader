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
    public void CanHandle_CardInstanceAddedToCollection_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new CardInstanceAddedToCollection(CardInstanceId.New(), CollectionId.New())));

    [Fact]
    public void CanHandle_CardInstanceRemovedFromCollection_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new CardInstanceRemovedFromCollection(CardInstanceId.New(), CollectionId.New())));

    [Fact]
    public void CanHandle_UnrelatedEvent_ReturnsFalse()
        => Assert.False(_sut.CanHandle(new CollectionCreated(CollectionId.New(), "x", UserId.New())));

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

    // ── CardInstanceAddedToCollection ────────────────────────────────────────

    [Fact]
    public async Task CardInstanceAddedToCollection_WritesCollectionTuple()
    {
        var instanceId = CardInstanceId.New();
        var collectionId = CollectionId.New();

        await _sut.HandleAsync(new CardInstanceAddedToCollection(instanceId, collectionId));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"{FgaTypes.Collection}:{collectionId}" &&
                t[0].Relation == FgaRelations.Collection &&
                t[0].Object == $"{FgaTypes.CardInstance}:{instanceId}"),
            Arg.Any<CancellationToken>());
    }

    // ── CardInstanceRemovedFromCollection ────────────────────────────────────

    [Fact]
    public async Task CardInstanceRemovedFromCollection_DeletesCollectionTuple()
    {
        var instanceId = CardInstanceId.New();
        var collectionId = CollectionId.New();

        await _sut.HandleAsync(new CardInstanceRemovedFromCollection(instanceId, collectionId));

        await _fga.Received(1).DeleteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKeyWithoutCondition>>(t =>
                t.Count == 1 &&
                t[0].User == $"{FgaTypes.Collection}:{collectionId}" &&
                t[0].Relation == FgaRelations.Collection &&
                t[0].Object == $"{FgaTypes.CardInstance}:{instanceId}"),
            Arg.Any<CancellationToken>());
    }
}
