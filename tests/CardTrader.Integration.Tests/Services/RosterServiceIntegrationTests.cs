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

    // ── CanViewAsync / CanManageAsync ─────────────────────────────────────────

    [Fact]
    public async Task CanViewAsync_Owner_ReturnsTrue()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);

        Assert.True(await Service(scope).CanViewAsync(id, owner));
    }

    [Fact]
    public async Task CanViewAsync_Viewer_ReturnsTrue()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var viewer = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);
        await Service(scope).ShareWithUserAsync(id, owner, viewer);

        Assert.True(await Service(scope).CanViewAsync(id, viewer));
    }

    [Fact]
    public async Task CanViewAsync_Stranger_ReturnsFalse()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(id, "Test", UserId.New());

        Assert.False(await Service(scope).CanViewAsync(id, stranger));
    }

    [Fact]
    public async Task CanManageAsync_Owner_ReturnsTrue()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);

        Assert.True(await Service(scope).CanManageAsync(id, owner));
    }

    [Fact]
    public async Task CanManageAsync_ViewerOnly_ReturnsFalse()
    {
        // Viewer gets can_view but NOT can_manage — share does not escalate to ownership.
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var viewer = UserId.New();

        await Service(scope).CreateAsync(id, "Test", owner);
        await Service(scope).ShareWithUserAsync(id, owner, viewer);

        Assert.False(await Service(scope).CanManageAsync(id, viewer));
    }

    // ── GetAllVisibleAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllVisibleAsync_IncludesOwnedRosters()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();

        await Service(scope).CreateAsync(id, "Mine", owner);

        var visible = await Service(scope).GetAllVisibleAsync(owner);

        Assert.Contains(visible, r => r.Id == id);
    }

    [Fact]
    public async Task GetAllVisibleAsync_IncludesSharedRosters()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var viewer = UserId.New();

        await Service(scope).CreateAsync(id, "Shared", owner);
        await Service(scope).ShareWithUserAsync(id, owner, viewer);

        var visible = await Service(scope).GetAllVisibleAsync(viewer);

        Assert.Contains(visible, r => r.Id == id);
    }

    [Fact]
    public async Task GetAllVisibleAsync_IncludesPublicRosters()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var anyone = UserId.New();

        await Service(scope).CreateAsync(id, "Public", owner);
        await Service(scope).MakePublicAsync(id, owner);

        var visible = await Service(scope).GetAllVisibleAsync(anyone);

        Assert.Contains(visible, r => r.Id == id);
    }

    [Fact]
    public async Task GetAllVisibleAsync_ExcludesPrivateRosterOfOtherUser()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(id, "Private", UserId.New());

        var visible = await Service(scope).GetAllVisibleAsync(stranger);

        Assert.DoesNotContain(visible, r => r.Id == id);
    }

    [Fact]
    public async Task GetAllVisibleAsync_AfterRevoke_NoLongerIncludes()
    {
        using var scope = fixture.CreateScope();
        var id = RosterId.New();
        var owner = UserId.New();
        var viewer = UserId.New();

        await Service(scope).CreateAsync(id, "Revoked", owner);
        await Service(scope).ShareWithUserAsync(id, owner, viewer);
        Assert.Contains(await Service(scope).GetAllVisibleAsync(viewer), r => r.Id == id);

        await Service(scope).UnshareAsync(id, owner, viewer);

        var visible = await Service(scope).GetAllVisibleAsync(viewer);
        Assert.DoesNotContain(visible, r => r.Id == id);
    }
}
