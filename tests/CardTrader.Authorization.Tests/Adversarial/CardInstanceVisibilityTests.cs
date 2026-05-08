using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;

namespace CardTrader.Authorization.Tests.Adversarial;

[Collection(OpenFgaCollection.Name)]
public sealed class CardInstanceVisibilityTests(OpenFgaFixture fixture)
    : FgaTestBase(fixture)
{
    // ── Scenario A: Share → revoke via collection ────────────────────────────

    [Fact]
    public async Task CollectionViewer_CanSeeInstance_BeforeRevoke()
    {
        var (alice, bob, inst, coll) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.Collection}:{coll}"),
            T($"{FgaTypes.Collection}:{coll}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer,  $"{FgaTypes.Collection}:{coll}"));

        Assert.True(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    [Fact]
    public async Task CollectionViewer_CannotSeeInstance_AfterCollectionRevoke()
    {
        var (alice, bob, inst, coll) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.Collection}:{coll}"),
            T($"{FgaTypes.Collection}:{coll}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer,  $"{FgaTypes.Collection}:{coll}"));

        await DeleteAsync(D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.Collection}:{coll}"));

        Assert.False(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    // ── Scenario B: Public collection → private ──────────────────────────────

    [Fact]
    public async Task PublicCollection_AnyoneCanSeeInstance()
    {
        var (alice, eve, inst, coll) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,      $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,      $"{FgaTypes.Collection}:{coll}"),
            T($"{FgaTypes.Collection}:{coll}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T("user:*",        FgaRelations.Viewer,     $"{FgaTypes.Collection}:{coll}"));

        Assert.True(await CanAsync($"user:{eve}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    [Fact]
    public async Task MakingCollectionPrivate_RevokesPublicAccess()
    {
        var (alice, eve, inst, coll) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Collection}:{coll}"),
            T($"{FgaTypes.Collection}:{coll}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T("user:*",        FgaRelations.Viewer, $"{FgaTypes.Collection}:{coll}"));

        await DeleteAsync(D("user:*", FgaRelations.Viewer, $"{FgaTypes.Collection}:{coll}"));

        Assert.False(await CanAsync($"user:{eve}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    // ── Scenario C: Direct share survives collection revoke ───────────────────

    [Fact]
    public async Task DirectShare_PersistsAfterCollectionRevoke()
    {
        var (alice, bob, inst, coll) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.Collection}:{coll}"),
            T($"{FgaTypes.Collection}:{coll}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer,  $"{FgaTypes.Collection}:{coll}"),
            T($"user:{bob}",   FgaRelations.Viewer,  $"{FgaTypes.CardInstance}:{inst}"));

        // Revoke collection share only
        await DeleteAsync(D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.Collection}:{coll}"));

        // Direct share is still in effect
        Assert.True(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    [Fact]
    public async Task RevokingBothShares_DeniesAccess()
    {
        var (alice, bob, inst, coll) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.Collection}:{coll}"),
            T($"{FgaTypes.Collection}:{coll}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer,  $"{FgaTypes.Collection}:{coll}"),
            T($"user:{bob}",   FgaRelations.Viewer,  $"{FgaTypes.CardInstance}:{inst}"));

        await DeleteAsync(
            D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.Collection}:{coll}"),
            D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.CardInstance}:{inst}"));

        Assert.False(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    // ── Scenario D: Multi-collection — revoking one collection isn't enough ───

    [Fact]
    public async Task InstanceInTwoCollections_RevokingOne_StillDeniesIfOtherAlsoRevoked()
    {
        var (alice, bob, inst, collA, collB) = (Id(), Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.Collection}:{collA}"),
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.Collection}:{collB}"),
            T($"{FgaTypes.Collection}:{collA}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T($"{FgaTypes.Collection}:{collB}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer,  $"{FgaTypes.Collection}:{collA}"));

        // Revoke collA — bob was never on collB, so access disappears
        await DeleteAsync(D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.Collection}:{collA}"));

        Assert.False(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    [Fact]
    public async Task InstanceInTwoCollections_AccessViaSecondCollection_AfterFirstRevoked()
    {
        var (alice, bob, inst, collA, collB) = (Id(), Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.Collection}:{collA}"),
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.Collection}:{collB}"),
            T($"{FgaTypes.Collection}:{collA}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T($"{FgaTypes.Collection}:{collB}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer,  $"{FgaTypes.Collection}:{collA}"),
            T($"user:{bob}",   FgaRelations.Viewer,  $"{FgaTypes.Collection}:{collB}"));

        // Revoke only collA — bob retains access via collB
        await DeleteAsync(D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.Collection}:{collA}"));

        Assert.True(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    // ── Owner invariant ──────────────────────────────────────────────────────

    [Fact]
    public async Task Owner_CanAlwaysViewOwnInstance()
    {
        var (alice, inst) = (Id(), Id());
        await WriteAsync(T($"user:{alice}", FgaRelations.Owner, $"{FgaTypes.CardInstance}:{inst}"));

        Assert.True(await CanAsync($"user:{alice}", FgaRelations.CanView,  $"{FgaTypes.CardInstance}:{inst}"));
        Assert.True(await CanAsync($"user:{alice}", FgaRelations.CanTrade, $"{FgaTypes.CardInstance}:{inst}"));
    }

    [Fact]
    public async Task NonOwner_CannotTradeInstance_EvenWithViewAccess()
    {
        var (alice, bob, inst, coll) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,   $"{FgaTypes.Collection}:{coll}"),
            T($"{FgaTypes.Collection}:{coll}", FgaRelations.Collection, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer,  $"{FgaTypes.Collection}:{coll}"));

        Assert.True(await CanAsync($"user:{bob}", FgaRelations.CanView,  $"{FgaTypes.CardInstance}:{inst}"));
        Assert.False(await CanAsync($"user:{bob}", FgaRelations.CanTrade, $"{FgaTypes.CardInstance}:{inst}"));
    }
}
