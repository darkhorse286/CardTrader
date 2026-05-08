using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using NSubstitute;

namespace CardTrader.Application.Tests;

public sealed class CardAccessServiceTests
{
    private readonly IAuthorizationService _authz = Substitute.For<IAuthorizationService>();
    private readonly CardAccessService _sut;

    public CardAccessServiceTests() => _sut = new CardAccessService(_authz);

    // ── CanView ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanView_ForwardsDecision(bool allowed)
    {
        _authz.CheckAsync("user:alice", "can_view", "card_instance:card1", Arg.Any<CancellationToken>())
              .Returns(allowed);

        Assert.Equal(allowed, await _sut.CanViewAsync("alice", "card1"));
    }

    [Fact]
    public async Task CanView_BuildsCorrectFgaStrings()
    {
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(true);

        await _sut.CanViewAsync("user-123", "card-456");

        await _authz.Received(1).CheckAsync(
            "user:user-123", "can_view", "card_instance:card-456", Arg.Any<CancellationToken>());
    }

    // ── CanManage ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanManage_ForwardsDecision(bool allowed)
    {
        _authz.CheckAsync("user:alice", "can_manage", "card_instance:card1", Arg.Any<CancellationToken>())
              .Returns(allowed);

        Assert.Equal(allowed, await _sut.CanManageAsync("alice", "card1"));
    }

    [Fact]
    public async Task CanManage_BuildsCorrectFgaStrings()
    {
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);

        await _sut.CanManageAsync("user-123", "card-456");

        await _authz.Received(1).CheckAsync(
            "user:user-123", "can_manage", "card_instance:card-456", Arg.Any<CancellationToken>());
    }

    // ── CanTrade ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanTrade_ForwardsDecision(bool allowed)
    {
        _authz.CheckAsync("user:alice", "can_trade", "card_instance:card1", Arg.Any<CancellationToken>())
              .Returns(allowed);

        Assert.Equal(allowed, await _sut.CanTradeAsync("alice", "card1"));
    }

    [Fact]
    public async Task CanTrade_BuildsCorrectFgaStrings()
    {
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);

        await _sut.CanTradeAsync("user-123", "card-456");

        await _authz.Received(1).CheckAsync(
            "user:user-123", "can_trade", "card_instance:card-456", Arg.Any<CancellationToken>());
    }

    // ── Isolation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EachMethod_CallsCheckExactlyOnce()
    {
        _authz.CheckAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(true);

        await _sut.CanViewAsync("alice", "card1");
        await _sut.CanManageAsync("alice", "card1");
        await _sut.CanTradeAsync("alice", "card1");

        await _authz.Received(3).CheckAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
