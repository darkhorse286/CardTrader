using System.Security.Claims;
using CardTrader.Identity.Services;
using Microsoft.AspNetCore.Components.Authorization;
using NSubstitute;

namespace CardTrader.Identity.Tests;

public class CurrentUserTests
{
    private static AuthenticationState AuthenticatedState(Guid userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private static AuthenticationState AnonymousState()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));

    // ── GetUserIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserIdAsync_ReturnsUserId_WhenAuthenticated()
    {
        var expectedId = Guid.NewGuid();
        var provider = Substitute.For<AuthenticationStateProvider>();
        provider.GetAuthenticationStateAsync().Returns(AuthenticatedState(expectedId));
        var sut = new AspNetCoreCurrentUser(provider);

        var result = await sut.GetUserIdAsync();

        Assert.Equal(expectedId, result.Value);
    }

    [Fact]
    public async Task GetUserIdAsync_Throws_WhenNotAuthenticated()
    {
        var provider = Substitute.For<AuthenticationStateProvider>();
        provider.GetAuthenticationStateAsync().Returns(AnonymousState());
        var sut = new AspNetCoreCurrentUser(provider);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.GetUserIdAsync());
    }

    // ── IsAuthenticatedAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task IsAuthenticatedAsync_ReturnsTrue_WhenAuthenticated()
    {
        var provider = Substitute.For<AuthenticationStateProvider>();
        provider.GetAuthenticationStateAsync().Returns(AuthenticatedState(Guid.NewGuid()));
        var sut = new AspNetCoreCurrentUser(provider);

        Assert.True(await sut.IsAuthenticatedAsync());
    }

    [Fact]
    public async Task IsAuthenticatedAsync_ReturnsFalse_WhenAnonymous()
    {
        var provider = Substitute.For<AuthenticationStateProvider>();
        provider.GetAuthenticationStateAsync().Returns(AnonymousState());
        var sut = new AspNetCoreCurrentUser(provider);

        Assert.False(await sut.IsAuthenticatedAsync());
    }
}
