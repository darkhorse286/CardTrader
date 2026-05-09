using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.OpenFga;
using CardTrader.Infrastructure.TupleWriters;
using NSubstitute;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.Tests.TupleWriters;

public sealed class RosterTupleWriterTests
{
    private readonly IFgaWriteClient _fga = Substitute.For<IFgaWriteClient>();
    private readonly RosterTupleWriter _sut;

    public RosterTupleWriterTests() => _sut = new RosterTupleWriter(_fga);

    // ── CanHandle ────────────────────────────────────────────────────────────

    [Fact]
    public void CanHandle_RosterCreated_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new RosterCreated(RosterId.New(), "x", UserId.New())));

    [Fact]
    public void CanHandle_RosterSharedWithUser_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new RosterSharedWithUser(RosterId.New(), UserId.New())));

    [Fact]
    public void CanHandle_RosterUnshared_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new RosterUnshared(RosterId.New(), UserId.New())));

    [Fact]
    public void CanHandle_RosterMadePublic_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new RosterMadePublic(RosterId.New())));

    [Fact]
    public void CanHandle_RosterMadePrivate_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new RosterMadePrivate(RosterId.New())));

    [Fact]
    public void CanHandle_UnrelatedEvent_ReturnsFalse()
        => Assert.False(_sut.CanHandle(new CardInstanceCreated(CardInstanceId.New(), CardId.New(), UserId.New())));

    // ── RosterCreated ────────────────────────────────────────────────────────

    [Fact]
    public async Task RosterCreated_WritesOwnerTuple()
    {
        var id = RosterId.New();
        var ownerId = UserId.New();

        await _sut.HandleAsync(new RosterCreated(id, "My Lineup", ownerId));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{ownerId}" &&
                t[0].Relation == FgaRelations.Owner &&
                t[0].Object == $"{FgaTypes.Roster}:{id}"),
            Arg.Any<CancellationToken>());
    }

    // ── RosterSharedWithUser ─────────────────────────────────────────────────

    [Fact]
    public async Task RosterSharedWithUser_WritesViewerTuple()
    {
        var id = RosterId.New();
        var userId = UserId.New();

        await _sut.HandleAsync(new RosterSharedWithUser(id, userId));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{userId}" &&
                t[0].Relation == FgaRelations.Viewer &&
                t[0].Object == $"{FgaTypes.Roster}:{id}"),
            Arg.Any<CancellationToken>());
    }

    // ── RosterUnshared ───────────────────────────────────────────────────────

    [Fact]
    public async Task RosterUnshared_DeletesViewerTuple()
    {
        var id = RosterId.New();
        var userId = UserId.New();

        await _sut.HandleAsync(new RosterUnshared(id, userId));

        await _fga.Received(1).DeleteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKeyWithoutCondition>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{userId}" &&
                t[0].Relation == FgaRelations.Viewer &&
                t[0].Object == $"{FgaTypes.Roster}:{id}"),
            Arg.Any<CancellationToken>());
    }

    // ── RosterMadePublic ─────────────────────────────────────────────────────

    [Fact]
    public async Task RosterMadePublic_WritesWildcardViewerTuple()
    {
        var id = RosterId.New();

        await _sut.HandleAsync(new RosterMadePublic(id));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == "user:*" &&
                t[0].Relation == FgaRelations.Viewer &&
                t[0].Object == $"{FgaTypes.Roster}:{id}"),
            Arg.Any<CancellationToken>());
    }

    // ── RosterMadePrivate ────────────────────────────────────────────────────

    [Fact]
    public async Task RosterMadePrivate_DeletesWildcardViewerTuple()
    {
        var id = RosterId.New();

        await _sut.HandleAsync(new RosterMadePrivate(id));

        await _fga.Received(1).DeleteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKeyWithoutCondition>>(t =>
                t.Count == 1 &&
                t[0].User == "user:*" &&
                t[0].Relation == FgaRelations.Viewer &&
                t[0].Object == $"{FgaTypes.Roster}:{id}"),
            Arg.Any<CancellationToken>());
    }
}
