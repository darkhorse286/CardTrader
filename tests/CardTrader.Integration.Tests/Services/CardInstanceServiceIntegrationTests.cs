using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.Entities;
using CardTrader.Domain.Repositories;
using CardTrader.Domain.ValueObjects;
using CardTrader.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Integration.Tests.Services;

[Collection(IntegrationCollection.Name)]
public class CardInstanceServiceIntegrationTests(IntegrationFixture fixture)
{
    private CardInstanceService Service(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<CardInstanceService>();

    private RosterService RosterSvc(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<RosterService>();

    private IAuthorizationService Authz(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

    private Task<bool> CanView(IServiceScope scope, UserId user, CardInstanceId instance) =>
        Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{user}",
            FgaRelations.CanView,
            $"{FgaTypes.CardInstance}:{instance}");

    private Task<bool> CanManage(IServiceScope scope, UserId user, CardInstanceId instance) =>
        Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{user}",
            FgaRelations.CanManage,
            $"{FgaTypes.CardInstance}:{instance}");

    private async Task<CardId> SeedCardAsync(IServiceScope scope)
    {
        var cardId = CardId.New();
        var repo = scope.ServiceProvider.GetRequiredService<ICardRepository>();
        await repo.AddAsync(Card.Create(cardId, "Test Card", "Test Set", "Common", "Test Player", printRun: 1200));
        return cardId;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_OwnerCanManage()
    {
        using var scope = fixture.CreateScope();
        var cardId = await SeedCardAsync(scope);
        var id = CardInstanceId.New();
        var owner = UserId.New();

        await Service(scope).CreateAsync(id, cardId, owner, printNumber: 1);

        Assert.True(await CanManage(scope, owner, id));
    }

    [Fact]
    public async Task Create_OwnerCanView()
    {
        using var scope = fixture.CreateScope();
        var cardId = await SeedCardAsync(scope);
        var id = CardInstanceId.New();
        var owner = UserId.New();

        await Service(scope).CreateAsync(id, cardId, owner, printNumber: 2);

        Assert.True(await CanView(scope, owner, id));
    }

    [Fact]
    public async Task Create_StrangerCannotView()
    {
        using var scope = fixture.CreateScope();
        var cardId = await SeedCardAsync(scope);
        var id = CardInstanceId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(id, cardId, UserId.New(), printNumber: 3);

        Assert.False(await CanView(scope, stranger, id));
    }

    // ── AddToRosterAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddToRoster_RosterViewerCanViewInstance()
    {
        // Once a card instance is linked to a roster, anyone with can_view
        // on the roster also gains can_view on the instance (tuple-to-userset).
        using var scope = fixture.CreateScope();
        var cardId = await SeedCardAsync(scope);
        var instanceId = CardInstanceId.New();
        var rosterId = RosterId.New();
        var instanceOwner = UserId.New();
        var rosterOwner = UserId.New();
        var viewer = UserId.New();

        await RosterSvc(scope).CreateAsync(rosterId, "Test", rosterOwner);
        await RosterSvc(scope).ShareWithUserAsync(rosterId, rosterOwner, viewer);
        await Service(scope).CreateAsync(instanceId, cardId, instanceOwner, printNumber: 1);

        Assert.False(await CanView(scope, viewer, instanceId)); // not linked yet

        await Service(scope).AddToRosterAsync(instanceId, instanceOwner, rosterId);

        Assert.True(await CanView(scope, viewer, instanceId));
    }

    [Fact]
    public async Task AddToRoster_ByStranger_Throws()
    {
        using var scope = fixture.CreateScope();
        var cardId = await SeedCardAsync(scope);
        var instanceId = CardInstanceId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(instanceId, cardId, UserId.New(), printNumber: 1);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Service(scope).AddToRosterAsync(instanceId, stranger, RosterId.New()));
    }

    // ── RemoveFromRosterAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveFromRoster_RevokesRosterViewerAccess()
    {
        using var scope = fixture.CreateScope();
        var cardId = await SeedCardAsync(scope);
        var instanceId = CardInstanceId.New();
        var rosterId = RosterId.New();
        var instanceOwner = UserId.New();
        var rosterOwner = UserId.New();
        var viewer = UserId.New();

        await RosterSvc(scope).CreateAsync(rosterId, "Test", rosterOwner);
        await RosterSvc(scope).ShareWithUserAsync(rosterId, rosterOwner, viewer);
        await Service(scope).CreateAsync(instanceId, cardId, instanceOwner, printNumber: 1);
        await Service(scope).AddToRosterAsync(instanceId, instanceOwner, rosterId);
        Assert.True(await CanView(scope, viewer, instanceId)); // confirm linked

        await Service(scope).RemoveFromRosterAsync(instanceId, instanceOwner, rosterId);

        Assert.False(await CanView(scope, viewer, instanceId));
    }

    [Fact]
    public async Task RemoveFromRoster_ByStranger_Throws()
    {
        using var scope = fixture.CreateScope();
        var cardId = await SeedCardAsync(scope);
        var instanceId = CardInstanceId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(instanceId, cardId, UserId.New(), printNumber: 1);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Service(scope).RemoveFromRosterAsync(instanceId, stranger, RosterId.New()));
    }
}
