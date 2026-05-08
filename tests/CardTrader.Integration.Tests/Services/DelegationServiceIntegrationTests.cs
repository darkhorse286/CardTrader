using CardTrader.Application.Abstractions;
using CardTrader.Application.Services;
using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;
using CardTrader.Domain.ValueObjects;
using CardTrader.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CardTrader.Integration.Tests.Services;

[Collection(IntegrationCollection.Name)]
public class DelegationServiceIntegrationTests(IntegrationFixture fixture)
{
    private DelegationService Service(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<DelegationService>();

    private IAuthorizationService Authz(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

    private Task<bool> HasRelation(IServiceScope scope, UserId user, string relation, DelegationId delegation) =>
        Authz(scope).CheckAsync(
            $"{FgaTypes.User}:{user}",
            relation,
            $"{FgaTypes.Delegation}:{delegation}");

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_DelegatorHasDelegatorRelation()
    {
        using var scope = fixture.CreateScope();
        var id = DelegationId.New();
        var delegator = UserId.New();
        var delegatee = UserId.New();

        await Service(scope).CreateAsync(id, delegator, delegatee);

        Assert.True(await HasRelation(scope, delegator, FgaRelations.Delegator, id));
    }

    [Fact]
    public async Task Create_DelegateeHasDelegateeRelation()
    {
        using var scope = fixture.CreateScope();
        var id = DelegationId.New();
        var delegator = UserId.New();
        var delegatee = UserId.New();

        await Service(scope).CreateAsync(id, delegator, delegatee);

        Assert.True(await HasRelation(scope, delegatee, FgaRelations.Delegatee, id));
        Assert.False(await HasRelation(scope, delegatee, FgaRelations.Delegator, id));
    }

    // ── ActivateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Activate_GrantsActiveDelegatorRelation()
    {
        using var scope = fixture.CreateScope();
        var id = DelegationId.New();
        var delegator = UserId.New();
        var delegatee = UserId.New();

        await Service(scope).CreateAsync(id, delegator, delegatee);
        await Service(scope).ActivateAsync(id, delegator);

        Assert.True(await HasRelation(scope, delegator, FgaRelations.ActiveDelegator, id));
    }

    [Fact]
    public async Task Activate_ByDelegatee_Throws()
    {
        // Adversarial: the delegatee cannot activate their own delegation —
        // only the delegator controls when the relationship becomes active.
        using var scope = fixture.CreateScope();
        var id = DelegationId.New();
        var delegator = UserId.New();
        var delegatee = UserId.New();

        await Service(scope).CreateAsync(id, delegator, delegatee);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Service(scope).ActivateAsync(id, delegatee));
    }

    [Fact]
    public async Task Activate_ByStranger_Throws()
    {
        using var scope = fixture.CreateScope();
        var id = DelegationId.New();
        var stranger = UserId.New();

        await Service(scope).CreateAsync(id, UserId.New(), UserId.New());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Service(scope).ActivateAsync(id, stranger));
    }

    // ── RevokeAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Revoke_RemovesActiveDelegatorRelation()
    {
        // Adversarial: after revocation the delegator no longer has
        // active_delegator — any supervisor access they derived from it is gone.
        using var scope = fixture.CreateScope();
        var id = DelegationId.New();
        var delegator = UserId.New();
        var delegatee = UserId.New();

        await Service(scope).CreateAsync(id, delegator, delegatee);
        await Service(scope).ActivateAsync(id, delegator);
        Assert.True(await HasRelation(scope, delegator, FgaRelations.ActiveDelegator, id));

        await Service(scope).RevokeAsync(id, delegator);

        Assert.False(await HasRelation(scope, delegator, FgaRelations.ActiveDelegator, id));
    }
}
