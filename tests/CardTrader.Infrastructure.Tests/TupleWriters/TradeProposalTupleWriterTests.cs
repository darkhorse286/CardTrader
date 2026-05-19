using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Events;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.OpenFga;
using CardTrader.Infrastructure.TupleWriters;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenFga.Sdk.Client.Model;

namespace CardTrader.Infrastructure.Tests.TupleWriters;

public sealed class TradeProposalTupleWriterTests
{
    private readonly IFgaWriteClient _fga = Substitute.For<IFgaWriteClient>();
    private readonly TradeProposalTupleWriter _sut;

    public TradeProposalTupleWriterTests()
    {
        var delegRepo = Substitute.For<IDelegationRepository>();
        delegRepo.GetByDelegateeAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Delegation>>([]));

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelegationRepository)).Returns(delegRepo);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _sut = new TradeProposalTupleWriter(_fga, scopeFactory);
    }

    // ── CanHandle ────────────────────────────────────────────────────────────

    [Fact]
    public void CanHandle_TradeProposalCreated_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new TradeProposalCreated(TradeProposalId.New(), UserId.New(), UserId.New())));

    [Fact]
    public void CanHandle_TradeProposalFacilitatorAssigned_ReturnsTrue()
        => Assert.True(_sut.CanHandle(new TradeProposalFacilitatorAssigned(TradeProposalId.New(), UserId.New())));

    [Fact]
    public void CanHandle_TradeProposalAccepted_ReturnsFalse()
        => Assert.False(_sut.CanHandle(new TradeProposalAccepted(TradeProposalId.New())));

    [Fact]
    public void CanHandle_TradeProposalCancelled_ReturnsFalse()
        => Assert.False(_sut.CanHandle(new TradeProposalCancelled(TradeProposalId.New())));

    // ── TradeProposalCreated ─────────────────────────────────────────────────

    [Fact]
    public async Task TradeProposalCreated_WritesBothInitiatorAndRecipientTuples()
    {
        var id = TradeProposalId.New();
        var initiatorId = UserId.New();
        var recipientId = UserId.New();

        await _sut.HandleAsync(new TradeProposalCreated(id, initiatorId, recipientId));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 2 &&
                t.Any(x => x.User == $"user:{initiatorId}" && x.Relation == FgaRelations.Initiator && x.Object == $"{FgaTypes.TradeProposal}:{id}") &&
                t.Any(x => x.User == $"user:{recipientId}" && x.Relation == FgaRelations.Recipient && x.Object == $"{FgaTypes.TradeProposal}:{id}")),
            Arg.Any<CancellationToken>());
    }

    // ── TradeProposalFacilitatorAssigned ─────────────────────────────────────

    [Fact]
    public async Task TradeProposalFacilitatorAssigned_WritesFacilitatorTuple()
    {
        var id = TradeProposalId.New();
        var facilitatorId = UserId.New();

        await _sut.HandleAsync(new TradeProposalFacilitatorAssigned(id, facilitatorId));

        await _fga.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<ClientTupleKey>>(t =>
                t.Count == 1 &&
                t[0].User == $"user:{facilitatorId}" &&
                t[0].Relation == FgaRelations.Facilitator &&
                t[0].Object == $"{FgaTypes.TradeProposal}:{id}"),
            Arg.Any<CancellationToken>());
    }

    // ── No write on terminal events ──────────────────────────────────────────

    [Fact]
    public async Task TradeProposalAccepted_WritesNothing()
    {
        await _sut.HandleAsync(new TradeProposalAccepted(TradeProposalId.New()));

        await _fga.DidNotReceive().WriteAsync(Arg.Any<IReadOnlyList<ClientTupleKey>>(), Arg.Any<CancellationToken>());
        await _fga.DidNotReceive().DeleteAsync(Arg.Any<IReadOnlyList<ClientTupleKeyWithoutCondition>>(), Arg.Any<CancellationToken>());
    }
}
