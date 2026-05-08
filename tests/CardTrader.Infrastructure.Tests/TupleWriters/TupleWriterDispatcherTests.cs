using CardTrader.Application.Abstractions;
using CardTrader.Domain.Events;
using CardTrader.Domain.ValueObjects;
using CardTrader.Infrastructure.TupleWriters;
using NSubstitute;
using CardTrader.Domain.Common;

namespace CardTrader.Infrastructure.Tests.TupleWriters;

public sealed class TupleWriterDispatcherTests
{
    private readonly ITupleWriterHandler _handlerA = Substitute.For<ITupleWriterHandler>();
    private readonly ITupleWriterHandler _handlerB = Substitute.For<ITupleWriterHandler>();
    private readonly IDomainEventDispatcher _sut;

    public TupleWriterDispatcherTests()
    {
        _sut = new TupleWriterDispatcher([_handlerA, _handlerB]);
    }

    // ── Routing ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_RoutesEventToMatchingHandler()
    {
        var @event = new CollectionCreated(CollectionId.New(), "x", UserId.New());
        _handlerA.CanHandle(@event).Returns(true);

        await _sut.DispatchAsync([@event]);

        await _handlerA.Received(1).HandleAsync(@event, Arg.Any<CancellationToken>());
        await _handlerB.DidNotReceive().HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_FirstMatchingHandlerWins()
    {
        var @event = new CollectionCreated(CollectionId.New(), "x", UserId.New());
        _handlerA.CanHandle(@event).Returns(true);
        _handlerB.CanHandle(@event).Returns(true);

        await _sut.DispatchAsync([@event]);

        await _handlerA.Received(1).HandleAsync(@event, Arg.Any<CancellationToken>());
        await _handlerB.DidNotReceive().HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_NoMatchingHandler_IsIgnoredSilently()
    {
        var @event = new TradeProposalAccepted(TradeProposalId.New());
        _handlerA.CanHandle(@event).Returns(false);
        _handlerB.CanHandle(@event).Returns(false);

        // Should not throw.
        await _sut.DispatchAsync([@event]);

        await _handlerA.DidNotReceive().HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        await _handlerB.DidNotReceive().HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    // ── Multiple events ──────────────────────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_MultipleEvents_EachRoutedIndependently()
    {
        var eventA = new CollectionCreated(CollectionId.New(), "x", UserId.New());
        var eventB = new CardInstanceCreated(CardInstanceId.New(), CardId.New(), UserId.New());

        _handlerA.CanHandle(eventA).Returns(true);
        _handlerA.CanHandle(eventB).Returns(false);
        _handlerB.CanHandle(eventA).Returns(false);
        _handlerB.CanHandle(eventB).Returns(true);

        await _sut.DispatchAsync([eventA, eventB]);

        await _handlerA.Received(1).HandleAsync(eventA, Arg.Any<CancellationToken>());
        await _handlerB.Received(1).HandleAsync(eventB, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_EmptyList_CallsNoHandlers()
    {
        await _sut.DispatchAsync([]);

        await _handlerA.DidNotReceive().HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        await _handlerB.DidNotReceive().HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    // ── CancellationToken propagation ────────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_PropagatesCancellationToken()
    {
        var @event = new CollectionCreated(CollectionId.New(), "x", UserId.New());
        _handlerA.CanHandle(@event).Returns(true);
        var cts = new CancellationTokenSource();

        await _sut.DispatchAsync([@event], cts.Token);

        await _handlerA.Received(1).HandleAsync(@event, cts.Token);
    }
}
