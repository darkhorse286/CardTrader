using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.ValueObjects;
using CardTrader.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Integration.Tests.Services;

[Collection(IntegrationCollection.Name)]
public class RosterServiceIntegrationTests(IntegrationFixture fixture)
{
    private RosterService Service(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<RosterService>();

    private IAuthorizationService Authz(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

    private Task<bool> CanView(IServiceScope scope, UserId user, RosterId roster) =>
        Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{user}",
            FgaRelations.CanView,
            $"{FgaTypes.Roster}:{roster}");

    private Task<bool> CanManage(IServiceScope scope, UserId user, RosterId roster) =>
        Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{user}",
            FgaRelations.CanManage,
            $"{FgaTypes.Roster}:{roster}");

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_OwnerCanManage()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);

        await Service(scope).ShareWithUserAsync(id, owner, UserId.New());
    }

    [Fact]
    public async Task Create_StrangerCannotManage()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Service(scope).ShareWithUserAsync(id, stranger, UserId.New()));
    }

    // ── ShareWithUser ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ShareWithUser_GrantsViewAccess()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var viewer = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);
        await Service(scope).ShareWithUserAsync(id, owner, viewer);

        Assert.True(await CanView(scope, viewer, id));
        Assert.False(await CanManage(scope, viewer, id));
    }

    [Fact]
    public async Task ShareWithUser_ByStranger_Throws()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Service(scope).ShareWithUserAsync(id, stranger, UserId.New()));
    }

    // ── Unshare ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unshare_RevokesViewAccess()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var viewer = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);
        await Service(scope).ShareWithUserAsync(id, owner, viewer);
        Assert.True(await CanView(scope, viewer, id));

        await Service(scope).UnshareAsync(id, owner, viewer);

        Assert.False(await CanView(scope, viewer, id));
    }

    // ── MakePublic / MakePrivate ──────────────────────────────────────────────

    [Fact]
    public async Task MakePublic_AnyUserCanView()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var anyone = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);
        await Service(scope).MakePublicAsync(id, owner);

        Assert.True(await CanView(scope, anyone, id));
    }

    [Fact]
    public async Task MakePrivate_RemovesPublicViewAccess()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var anyone = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);
        await Service(scope).MakePublicAsync(id, owner);
        Assert.True(await CanView(scope, anyone, id));

        await Service(scope).MakePrivateAsync(id, owner);

        Assert.False(await CanView(scope, anyone, id));
    }
}
