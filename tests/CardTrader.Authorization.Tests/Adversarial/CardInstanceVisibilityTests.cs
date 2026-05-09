using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;

namespace CardTrader.Authorization.Tests.Adversarial;

[Collection(OpenFgaCollection.Name)]
public sealed class CardInstanceVisibilityTests(OpenFgaFixture fixture)
    : FgaTestBase(fixture)
{
    // ── Scenario A: Share → revoke via roster ────────────────────────────────

    [Fact]
    public async Task RosterViewer_CanSeeInstance_BeforeRevoke()
    {
        var (alice, bob, inst, roster) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{roster}"),
            T($"{FgaTypes.Roster}:{roster}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"));

        Assert.True(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    [Fact]
    public async Task RosterViewer_CannotSeeInstance_AfterRosterRevoke()
    {
        var (alice, bob, inst, roster) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{roster}"),
            T($"{FgaTypes.Roster}:{roster}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"));

        await DeleteAsync(D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"));

        Assert.False(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    // ── Scenario B: Public roster → private ──────────────────────────────────

    [Fact]
    public async Task PublicRoster_AnyoneCanSeeInstance()
    {
        var (alice, eve, inst, roster) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{roster}"),
            T($"{FgaTypes.Roster}:{roster}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T("user:*",        FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"));

        Assert.True(await CanAsync($"user:{eve}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    [Fact]
    public async Task MakingRosterPrivate_RevokesPublicAccess()
    {
        var (alice, eve, inst, roster) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{roster}"),
            T($"{FgaTypes.Roster}:{roster}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T("user:*",        FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"));

        await DeleteAsync(D("user:*", FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"));

        Assert.False(await CanAsync($"user:{eve}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    // ── Scenario C: Direct share survives roster revoke ───────────────────────

    [Fact]
    public async Task DirectShare_PersistsAfterRosterRevoke()
    {
        var (alice, bob, inst, roster) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{roster}"),
            T($"{FgaTypes.Roster}:{roster}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"),
            T($"user:{bob}",   FgaRelations.Viewer, $"{FgaTypes.CardInstance}:{inst}"));

        // Revoke roster share only
        await DeleteAsync(D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"));

        // Direct share is still in effect
        Assert.True(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    [Fact]
    public async Task RevokingBothShares_DeniesAccess()
    {
        var (alice, bob, inst, roster) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{roster}"),
            T($"{FgaTypes.Roster}:{roster}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"),
            T($"user:{bob}",   FgaRelations.Viewer, $"{FgaTypes.CardInstance}:{inst}"));

        await DeleteAsync(
            D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"),
            D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.CardInstance}:{inst}"));

        Assert.False(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    // ── Scenario D: Multi-roster — revoking one isn't enough ──────────────────

    [Fact]
    public async Task InstanceInTwoRosters_RevokingOne_StillDeniesIfOtherAlsoRevoked()
    {
        var (alice, bob, inst, rosterA, rosterB) = (Id(), Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{rosterA}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{rosterB}"),
            T($"{FgaTypes.Roster}:{rosterA}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T($"{FgaTypes.Roster}:{rosterB}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer, $"{FgaTypes.Roster}:{rosterA}"));

        // Revoke rosterA — bob was never on rosterB, so access disappears
        await DeleteAsync(D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.Roster}:{rosterA}"));

        Assert.False(await CanAsync($"user:{bob}", FgaRelations.CanView, $"{FgaTypes.CardInstance}:{inst}"));
    }

    [Fact]
    public async Task InstanceInTwoRosters_AccessViaSecondRoster_AfterFirstRevoked()
    {
        var (alice, bob, inst, rosterA, rosterB) = (Id(), Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{rosterA}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{rosterB}"),
            T($"{FgaTypes.Roster}:{rosterA}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T($"{FgaTypes.Roster}:{rosterB}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer, $"{FgaTypes.Roster}:{rosterA}"),
            T($"user:{bob}",   FgaRelations.Viewer, $"{FgaTypes.Roster}:{rosterB}"));

        // Revoke only rosterA — bob retains access via rosterB
        await DeleteAsync(D($"user:{bob}", FgaRelations.Viewer, $"{FgaTypes.Roster}:{rosterA}"));

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
        var (alice, bob, inst, roster) = (Id(), Id(), Id(), Id());
        await WriteAsync(
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{alice}", FgaRelations.Owner,  $"{FgaTypes.Roster}:{roster}"),
            T($"{FgaTypes.Roster}:{roster}", FgaRelations.Roster, $"{FgaTypes.CardInstance}:{inst}"),
            T($"user:{bob}",   FgaRelations.Viewer, $"{FgaTypes.Roster}:{roster}"));

        Assert.True(await CanAsync($"user:{bob}", FgaRelations.CanView,  $"{FgaTypes.CardInstance}:{inst}"));
        Assert.False(await CanAsync($"user:{bob}", FgaRelations.CanTrade, $"{FgaTypes.CardInstance}:{inst}"));
    }
}
