using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.OpenFga;
using CardTrader.Infrastructure.TupleWriters;
using NSubstitute;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.Tests.TupleWriters;

public sealed class CollectionTupleWriterTests
{
    private readonly IFgaWriteClient _fga = Substitute.For<IFgaWriteClient>();
    private readonly CollectionTupleWriter _sut;

    public CollectionTupleWriterTests() => _sut = new CollectionTupleWriter(_fga);

    // ── CanHandle ────────────────────────────────────────────────────────────

    [Fact]
    public void CanHandle_CollectionCreated_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new CollectionCreated(CollectionId.New(), "x", UserId.New())));

    [Fact]
    public void CanHandle_CollectionSharedWithUser_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new CollectionSharedWithUser(CollectionId.New(), UserId.New())));

    [Fact]
    public void CanHandle_CollectionUnshared_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new CollectionUnshared(CollectionId.New(), UserId.New())));

    [Fact]
    public void CanHandle_CollectionMadePublic_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new CollectionMadePublic(CollectionId.New())));

    [Fact]
    public void CanHandle_CollectionMadePrivate_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new CollectionMadePrivate(CollectionId.New())));

    [Fact]
    public void CanHandle_UnrelatedEvent_ReturnsFalse()
        => Assert.False(_sut.CanHandle(new CardInstanceCreated(CardInstanceId.New(), CardId.New(), UserId.New())));

    // ── CollectionCreated ────────────────────────────────────────────────────

    [Fact]
    public async Task CollectionCreated_WritesOwnerTuple()
    {
        var id = CollectionId.New();
        var ownerId = UserId.New();

        await _sut.HandleAsync(new CollectionCreated(id, "My Cards", ownerId));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{ownerId}" &&
                t[0].Relation == FgaRelations.Owner &&
                t[0].Object == $"{FgaTypes.Collection}:{id}"),
            Arg.Any<CancellationToken>());
    }

    // ── CollectionSharedWithUser ─────────────────────────────────────────────

    [Fact]
    public async Task CollectionSharedWithUser_WritesViewerTuple()
    {
        var id = CollectionId.New();
        var userId = UserId.New();

        await _sut.HandleAsync(new CollectionSharedWithUser(id, userId));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{userId}" &&
                t[0].Relation == FgaRelations.Viewer &&
                t[0].Object == $"{FgaTypes.Collection}:{id}"),
            Arg.Any<CancellationToken>());
    }

    // ── CollectionUnshared ───────────────────────────────────────────────────

    [Fact]
    public async Task CollectionUnshared_DeletesViewerTuple()
    {
        var id = CollectionId.New();
        var userId = UserId.New();

        await _sut.HandleAsync(new CollectionUnshared(id, userId));

        await _fga.Received(1).DeleteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKeyWithoutCondition>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{userId}" &&
                t[0].Relation == FgaRelations.Viewer &&
                t[0].Object == $"{FgaTypes.Collection}:{id}"),
            Arg.Any<CancellationToken>());
    }

    // ── CollectionMadePublic ─────────────────────────────────────────────────

    [Fact]
    public async Task CollectionMadePublic_WritesWildcardViewerTuple()
    {
        var id = CollectionId.New();

        await _sut.HandleAsync(new CollectionMadePublic(id));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == "user:*" &&
                t[0].Relation == FgaRelations.Viewer &&
                t[0].Object == $"{FgaTypes.Collection}:{id}"),
            Arg.Any<CancellationToken>());
    }

    // ── CollectionMadePrivate ────────────────────────────────────────────────

    [Fact]
    public async Task CollectionMadePrivate_DeletesWildcardViewerTuple()
    {
        var id = CollectionId.New();

        await _sut.HandleAsync(new CollectionMadePrivate(id));

        await _fga.Received(1).DeleteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKeyWithoutCondition>>(t =>
                t.Count == 1 &&
                t[0].User == "user:*" &&
                t[0].Relation == FgaRelations.Viewer &&
                t[0].Object == $"{FgaTypes.Collection}:{id}"),
            Arg.Any<CancellationToken>());
    }
}
