using CardTrader.Authorization.Conditions;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.OpenFga;
using CardTrader.Infrastructure.TupleWriters;
using NSubstitute;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.Tests.TupleWriters;

public sealed class DelegationTupleWriterTests
{
    private readonly IFgaWriteClient _fga = Substitute.For<IFgaWriteClient>();
    private readonly DelegationTupleWriter _sut;

    public DelegationTupleWriterTests() => _sut = new DelegationTupleWriter(_fga);

    // ── CanHandle ────────────────────────────────────────────────────────────

    [Fact]
    public void CanHandle_DelegationCreated_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new DelegationCreated(DelegationId.New(), UserId.New(), UserId.New())));

    [Fact]
    public void CanHandle_DelegationActivated_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new DelegationActivated(DelegationId.New(), UserId.New(), null)));

    [Fact]
    public void CanHandle_DelegationRevoked_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new DelegationRevoked(DelegationId.New(), UserId.New())));

    [Fact]
    public void CanHandle_UnrelatedEvent_ReturnsFalse()
        => Assert.False(_sut.CanHandle(new RosterCreated(RosterId.New(), "x", UserId.New())));

    // ── DelegationCreated ────────────────────────────────────────────────────

    [Fact]
    public async Task DelegationCreated_WritesDelegatorAndDelegateeTuples()
    {
        var id = DelegationId.New();
        var delegatorId = UserId.New();
        var delegateeId = UserId.New();

        await _sut.HandleAsync(new DelegationCreated(id, delegatorId, delegateeId));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 2 &&
                t.Any(x => x.User == $"user:{delegatorId}" && x.Relation == FgaRelations.Delegator && x.Object == $"{FgaTypes.Delegation}:{id}") &&
                t.Any(x => x.User == $"user:{delegateeId}" && x.Relation == FgaRelations.Delegatee && x.Object == $"{FgaTypes.Delegation}:{id}")),
            Arg.Any<CancellationToken>());
    }

    // ── DelegationActivated (indefinite) ─────────────────────────────────────

    [Fact]
    public async Task DelegationActivated_WithoutExpiry_WritesUnconditionedTuple()
    {
        var id = DelegationId.New();
        var delegatorId = UserId.New();

        await _sut.HandleAsync(new DelegationActivated(id, delegatorId, ExpiresAt: null));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{delegatorId}" &&
                t[0].Relation == FgaRelations.ActiveDelegator &&
                t[0].Object == $"{FgaTypes.Delegation}:{id}" &&
                t[0].Condition == null),
            Arg.Any<CancellationToken>());
    }

    // ── DelegationActivated (time-bound) ─────────────────────────────────────

    [Fact]
    public async Task DelegationActivated_WithExpiry_WritesConditionedTuple()
    {
        var id = DelegationId.New();
        var delegatorId = UserId.New();
        var expiresAt = DateTimeOffset.UtcNow.AddYears(1);

        await _sut.HandleAsync(new DelegationActivated(id, delegatorId, expiresAt));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{delegatorId}" &&
                t[0].Relation == FgaRelations.ActiveDelegator &&
                t[0].Object == $"{FgaTypes.Delegation}:{id}" &&
                t[0].Condition != null &&
                t[0].Condition!.Name == FgaConditions.NotExpired &&
                t[0].Condition!.Context != null),
            Arg.Any<CancellationToken>());
    }

    // ── DelegationRevoked ────────────────────────────────────────────────────

    [Fact]
    public async Task DelegationRevoked_DeletesActiveDelegatorTuple()
    {
        var id = DelegationId.New();
        var delegatorId = UserId.New();

        await _sut.HandleAsync(new DelegationRevoked(id, delegatorId));

        await _fga.Received(1).DeleteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKeyWithoutCondition>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{delegatorId}" &&
                t[0].Relation == FgaRelations.ActiveDelegator &&
                t[0].Object == $"{FgaTypes.Delegation}:{id}"),
            Arg.Any<CancellationToken>());
    }
}
