using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Domain.Common;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using NSubstitute;

namespace CardTrader.Application.Tests;

public class TradeProposalServiceTests
{
    private readonly ITradeProposalRepository _proposals = Substitute.For<ITradeProposalRepository>();
    private readonly IAuthorizationService _authz = Substitute.For<IAuthorizationService>();
    private readonly IDomainEventDispatcher _dispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly TradeProposalService _sut;

    public TradeProposalServiceTests()
    {
        _sut = new TradeProposalService(_proposals, _authz, _dispatcher);
    }

    private static (TradeProposalId, UserId, UserId) MakeIds() =>
        (TradeProposalId.New(), UserId.New(), UserId.New());

    private static TradeProposal MakeProposal(TradeProposalId id, UserId initiator, UserId recipient)
    {
        var p = TradeProposal.Create(id, initiator, recipient);
        p.ClearDomainEvents();
        return p;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsAndDispatchesEvents()
    {
        var (id, initiator, recipient) = MakeIds();

        var result = await _sut.CreateAsync(id, initiator, recipient);

        await _proposals.Received().AddAsync(Arg.Is<TradeProposal>(p => p.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task CreateAsync_ClearsEventsAfterDispatch()
    {
        var (id, initiator, recipient) = MakeIds();

        var result = await _sut.CreateAsync(id, initiator, recipient);

        Assert.Empty(result.DomainEvents);
    }

    // ── AssignFacilitatorAsync ────────────────────────────────────────────────

    [Fact]
    public async Task AssignFacilitatorAsync_WhenAuthorized_DispatchesEvent()
    {
        var (id, initiator, recipient) = MakeIds();
        var facilitator = UserId.New();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        await _sut.AssignFacilitatorAsync(id, initiator, facilitator);

        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignFacilitatorAsync_WhenUnauthorized_Throws()
    {
        var (id, initiator, recipient) = MakeIds();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.AssignFacilitatorAsync(id, UserId.New(), UserId.New()));
    }

    [Fact]
    public async Task AssignFacilitatorAsync_WhenNotFound_Throws()
    {
        _proposals.GetByIdAsync(Arg.Any<TradeProposalId>(), Arg.Any<CancellationToken>()).Returns((TradeProposal?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AssignFacilitatorAsync(TradeProposalId.New(), UserId.New(), UserId.New()));
    }

    // ── AcceptAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AcceptAsync_WhenAuthorized_UpdatesAndDispatchesEvent()
    {
        var (id, initiator, recipient) = MakeIds();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        await _sut.AcceptAsync(id, recipient);

        await _proposals.Received().UpdateAsync(Arg.Is<TradeProposal>(p => p.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AcceptAsync_WhenUnauthorized_Throws()
    {
        var (id, initiator, recipient) = MakeIds();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.AcceptAsync(id, UserId.New()));
    }

    [Fact]
    public async Task AcceptAsync_WhenNotFound_Throws()
    {
        _proposals.GetByIdAsync(Arg.Any<TradeProposalId>(), Arg.Any<CancellationToken>()).Returns((TradeProposal?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.AcceptAsync(TradeProposalId.New(), UserId.New()));
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_WhenAuthorized_UpdatesAndDispatchesEvent()
    {
        var (id, initiator, recipient) = MakeIds();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        await _sut.CancelAsync(id, initiator);

        await _proposals.Received().UpdateAsync(Arg.Is<TradeProposal>(p => p.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WhenUnauthorized_Throws()
    {
        var (id, initiator, recipient) = MakeIds();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.CancelAsync(id, UserId.New()));
    }

    [Fact]
    public async Task CancelAsync_WhenNotFound_Throws()
    {
        _proposals.GetByIdAsync(Arg.Any<TradeProposalId>(), Arg.Any<CancellationToken>()).Returns((TradeProposal?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.CancelAsync(TradeProposalId.New(), UserId.New()));
    }
}
