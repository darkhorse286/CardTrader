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
    private readonly ICardInstanceRepository  _instances = Substitute.For<ICardInstanceRepository>();
    private readonly IAuthorizationService    _authz     = Substitute.For<IAuthorizationService>();
    private readonly IAdminService            _admin     = Substitute.For<IAdminService>();
    private readonly IDomainEventDispatcher   _dispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly TradeProposalService _sut;

    // Stable instance IDs used by MakeProposal so AcceptAsync can find them
    private static readonly CardInstanceId InitiatorInstanceId = CardInstanceId.New();
    private static readonly CardInstanceId RecipientInstanceId = CardInstanceId.New();

    public TradeProposalServiceTests()
    {
        _sut = new TradeProposalService(_proposals, _instances, _authz, _admin, _dispatcher);

        // Default: allow all authz checks; non-admin
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(true);
        _admin.IsAdminAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(false);

        // Default: return mock card instances for settlement
        SetupInstances(UserId.New(), UserId.New());
    }

    private static (TradeProposalId Id, UserId Initiator, UserId Recipient) MakeIds() =>
        (TradeProposalId.New(), UserId.New(), UserId.New());

    private static TradeProposal MakeProposal(TradeProposalId id, UserId initiator, UserId recipient)
    {
        var p = TradeProposal.Create(id, initiator, recipient, InitiatorInstanceId, RecipientInstanceId);
        p.ClearDomainEvents();
        return p;
    }

    private void SetupInstances(UserId initiator, UserId recipient)
    {
        var iInst = CardInstance.Create(InitiatorInstanceId, CardId.New(), initiator, 1, 100);
        var rInst = CardInstance.Create(RecipientInstanceId, CardId.New(), recipient, 2, 100);
        iInst.ClearDomainEvents(); rInst.ClearDomainEvents();
        _instances.GetByIdAsync(InitiatorInstanceId, Arg.Any<CancellationToken>()).Returns(iInst);
        _instances.GetByIdAsync(RecipientInstanceId, Arg.Any<CancellationToken>()).Returns(rInst);
    }

    // ── GetInvolvingUserAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetInvolvingUserAsync_DelegatesToRepository()
    {
        var userId = UserId.New();
        var (id, initiator, recipient) = MakeIds();
        var expected = new List<TradeProposal> { MakeProposal(id, initiator, recipient) };
        _proposals.GetInvolvingUserAsync(userId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetInvolvingUserAsync(userId);

        Assert.Equal(expected, result);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsAndDispatchesEvents()
    {
        var (id, initiator, recipient) = MakeIds();

        var result = await _sut.CreateAsync(id, initiator, recipient,
            InitiatorInstanceId, RecipientInstanceId);

        await _proposals.Received().AddAsync(Arg.Is<TradeProposal>(p => p.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task CreateAsync_ClearsEventsAfterDispatch()
    {
        var (id, initiator, recipient) = MakeIds();

        var result = await _sut.CreateAsync(id, initiator, recipient,
            InitiatorInstanceId, RecipientInstanceId);

        Assert.Empty(result.DomainEvents);
    }

    [Fact]
    public async Task CreateAsync_WhenInitiatorDoesNotOwnCard_Throws()
    {
        var (id, initiator, recipient) = MakeIds();
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.CreateAsync(id, initiator, recipient, InitiatorInstanceId, RecipientInstanceId));
    }

    // ── AcceptAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AcceptAsync_WhenAuthorized_UpdatesAndDispatchesEvent()
    {
        var (id, initiator, recipient) = MakeIds();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);
        SetupInstances(initiator, recipient);

        await _sut.AcceptAsync(id, recipient);

        await _proposals.Received().UpdateAsync(Arg.Is<TradeProposal>(p => p.Id == id), Arg.Any<CancellationToken>());
        await _dispatcher.Received().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AcceptAsync_SwapsOwnership()
    {
        var (id, initiator, recipient) = MakeIds();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);
        SetupInstances(initiator, recipient);

        await _sut.AcceptAsync(id, recipient);

        // Both instances are updated (ownership transferred)
        await _instances.Received(2).UpdateAsync(Arg.Any<CardInstance>(), Arg.Any<CancellationToken>());
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

    // ── AssignFacilitatorAsync ────────────────────────────────────────────────

    [Fact]
    public async Task AssignFacilitatorAsync_WhenAuthorized_DispatchesEvent()
    {
        var (id, initiator, recipient) = MakeIds();
        var facilitator = UserId.New();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);

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

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_WhenAuthorized_UpdatesAndDispatchesEvent()
    {
        var (id, initiator, recipient) = MakeIds();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);

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

    [Fact]
    public async Task CancelAsync_WhenAdmin_BypassesFgaAndCancels()
    {
        var (id, initiator, recipient) = MakeIds();
        var adminId = UserId.New();
        var proposal = MakeProposal(id, initiator, recipient);
        _proposals.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(proposal);
        _admin.IsAdminAsync(adminId, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.CancelAsync(id, adminId);

        await _proposals.Received().UpdateAsync(Arg.Is<TradeProposal>(p => p.Id == id), Arg.Any<CancellationToken>());
        await _authz.DidNotReceive().CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── GetAllPendingAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPendingAsync_WhenAdmin_ReturnsPendingProposals()
    {
        var adminId = UserId.New();
        var (id, initiator, recipient) = MakeIds();
        var expected = new List<TradeProposal> { MakeProposal(id, initiator, recipient) };
        _admin.IsAdminAsync(adminId, Arg.Any<CancellationToken>()).Returns(true);
        _proposals.GetAllPendingAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TradeProposal>>(expected));

        var result = await _sut.GetAllPendingAsync(adminId);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetAllPendingAsync_WhenNotAdmin_Throws()
    {
        _admin.IsAdminAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetAllPendingAsync(UserId.New()));
    }
}
