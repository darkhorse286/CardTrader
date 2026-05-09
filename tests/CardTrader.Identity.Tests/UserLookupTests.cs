using CardTrader.Domain.ValueObjects;
using CardTrader.Identity.Services;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace CardTrader.Identity.Tests;

public class UserLookupTests
{
    private static UserManager<CardTraderUser> MakeUserManager()
    {
        var store = Substitute.For<IUserStore<CardTraderUser>>();
        return Substitute.For<UserManager<CardTraderUser>>(
            store, null, null, null, null, null, null, null, null);
    }

    [Fact]
    public async Task FindByEmailAsync_ReturnsUserId_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var userManager = MakeUserManager();
        userManager.FindByEmailAsync("alice@example.com")
            .Returns(new CardTraderUser { Id = userId.ToString() });
        var sut = new AspNetCoreUserLookup(userManager);

        var result = await sut.FindByEmailAsync("alice@example.com");

        Assert.NotNull(result);
        Assert.Equal(userId, result.Value.Value);
    }

    [Fact]
    public async Task FindByEmailAsync_ReturnsNull_WhenUserNotFound()
    {
        var userManager = MakeUserManager();
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns((CardTraderUser?)null);
        var sut = new AspNetCoreUserLookup(userManager);

        var result = await sut.FindByEmailAsync("nobody@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetEmailByIdAsync_ReturnsEmail_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var userManager = MakeUserManager();
        userManager.FindByIdAsync(userId.ToString())
            .Returns(new CardTraderUser { Id = userId.ToString(), Email = "bob@example.com" });
        var sut = new AspNetCoreUserLookup(userManager);

        var result = await sut.GetEmailByIdAsync(new UserId(userId));

        Assert.Equal("bob@example.com", result);
    }

    [Fact]
    public async Task GetEmailByIdAsync_ReturnsNull_WhenUserNotFound()
    {
        var userManager = MakeUserManager();
        userManager.FindByIdAsync(Arg.Any<string>()).Returns((CardTraderUser?)null);
        var sut = new AspNetCoreUserLookup(userManager);

        var result = await sut.GetEmailByIdAsync(UserId.New());

        Assert.Null(result);
    }
}
