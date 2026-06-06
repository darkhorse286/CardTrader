using CardTrader.Authorization.Relations;
using CardTrader.Authorization.Types;

namespace CardTrader.Authorization.Tests.Adversarial;

[Collection(OpenFgaCollection.Name)]
public sealed class DelegationExpiryTests(OpenFgaFixture fixture)
    : FgaTestBase(fixture)
{
    private const string NotExpired = "not_expired";

    // Expiry stored in the tuple; current_time provided at check time.
    private const string ExpiresAt    = "2025-01-01T00:00:00Z";
    private const string BeforeExpiry = "2024-06-01T00:00:00Z";
    private const string AfterExpiry  = "2026-06-01T00:00:00Z";

    // ── Scenario E: Time-bound delegation ────────────────────────────────────

    [Fact]
    public async Task ActiveDelegator_CanViewDelegateTrade_BeforeExpiry()
    {
        var (parent, child, deleg, proposal) = (Id(), Id(), Id(), Id());
        await SetupDelegatedTradeProposal(parent, child, deleg, proposal, timed: true);

        var canView = await CanAsync(
            $"user:{parent}", FgaRelations.CanView, $"{FgaTypes.TradeProposal}:{proposal}",
            new { current_time = BeforeExpiry });

        Assert.True(canView);
    }

    [Fact]
    public async Task ActiveDelegator_CannotViewDelegateTrade_AfterExpiry()
    {
        var (parent, child, deleg, proposal) = (Id(), Id(), Id(), Id());
        await SetupDelegatedTradeProposal(parent, child, deleg, proposal, timed: true);

        var canView = await CanAsync(
            $"user:{parent}", FgaRelations.CanView, $"{FgaTypes.TradeProposal}:{proposal}",
            new { current_time = AfterExpiry });

        Assert.False(canView);
    }

    [Fact]
    public async Task IndefiniteDelegator_CanViewDelegateTrade_WithoutTimeContext()
    {
        var (parent, child, deleg, proposal) = (Id(), Id(), Id(), Id());
        await SetupDelegatedTradeProposal(parent, child, deleg, proposal, timed: false);

        // No time context needed — active_delegator is unconditional
        var canView = await CanAsync(
            $"user:{parent}", FgaRelations.CanView, $"{FgaTypes.TradeProposal}:{proposal}");

        Assert.True(canView);
    }

    [Fact]
    public async Task Delegator_CanCancelDelegateTrade_BeforeExpiry()
    {
        var (parent, child, deleg, proposal) = (Id(), Id(), Id(), Id());
        await SetupDelegatedTradeProposal(parent, child, deleg, proposal, timed: true);

        var canCancel = await CanAsync(
            $"user:{parent}", FgaRelations.CanCancel, $"{FgaTypes.TradeProposal}:{proposal}",
            new { current_time = BeforeExpiry });

        Assert.True(canCancel);
    }

    [Fact]
    public async Task Delegator_CannotAcceptDelegateTrade_EvenBeforeExpiry()
    {
        // Supervisory role gives view + cancel, NOT accept.
        var (parent, child, deleg, proposal) = (Id(), Id(), Id(), Id());
        await SetupDelegatedTradeProposal(parent, child, deleg, proposal, timed: true);

        var canAccept = await CanAsync(
            $"user:{parent}", FgaRelations.CanAccept, $"{FgaTypes.TradeProposal}:{proposal}",
            new { current_time = BeforeExpiry });

        Assert.False(canAccept);
    }

    [Fact]
    public async Task RevokedDelegation_DeniesAccessImmediately()
    {
        var (parent, child, deleg, proposal) = (Id(), Id(), Id(), Id());
        await SetupDelegatedTradeProposal(parent, child, deleg, proposal, timed: false);

        // Revoke — delete the active_delegator tuple
        await DeleteAsync(D($"user:{parent}", FgaRelations.ActiveDelegator, $"{FgaTypes.Delegation}:{deleg}"));

        Assert.False(await CanAsync(
            $"user:{parent}", FgaRelations.CanView, $"{FgaTypes.TradeProposal}:{proposal}"));
    }

    // ── Scenario E (ListObjects): time-bound delegation and ListObjects ──────
    //
    // ListObjectsAsync must pass current_time so that not_expired conditions
    // are evaluated correctly. Without it, expired delegations produce stale results.

    [Fact]
    public async Task ListObjects_BeforeExpiry_IncludesProposal()
    {
        var (parent, child, deleg, proposal) = (Id(), Id(), Id(), Id());
        await SetupDelegatedTradeProposal(parent, child, deleg, proposal, timed: true);

        var objects = await ListAsync(
            $"user:{parent}", FgaRelations.CanView, FgaTypes.TradeProposal,
            new { current_time = BeforeExpiry });

        Assert.Contains($"{FgaTypes.TradeProposal}:{proposal}", objects);
    }

    [Fact]
    public async Task ListObjects_AfterExpiry_ExcludesProposal()
    {
        var (parent, child, deleg, proposal) = (Id(), Id(), Id(), Id());
        await SetupDelegatedTradeProposal(parent, child, deleg, proposal, timed: true);

        var objects = await ListAsync(
            $"user:{parent}", FgaRelations.CanView, FgaTypes.TradeProposal,
            new { current_time = AfterExpiry });

        Assert.DoesNotContain($"{FgaTypes.TradeProposal}:{proposal}", objects);
    }

    // ── Setup helper ─────────────────────────────────────────────────────────

    private async Task SetupDelegatedTradeProposal(
        string parent, string child, string delegId, string proposalId, bool timed)
    {
        // Delegation tuples
        var delegObj = $"{FgaTypes.Delegation}:{delegId}";
        var activeDelegatorTuple = timed
            ? TWithCondition(
                $"user:{parent}", FgaRelations.ActiveDelegator, delegObj,
                NotExpired, new { expires_at = ExpiresAt })
            : T($"user:{parent}", FgaRelations.ActiveDelegator, delegObj);

        await WriteAsync(
            T($"user:{parent}", FgaRelations.Delegator,  delegObj),
            T($"user:{child}",  FgaRelations.Delegatee,  delegObj),
            activeDelegatorTuple);

        // Trade proposal by child, with supervisor pointing at the delegation
        await WriteAsync(
            T($"user:{child}",                                  FgaRelations.Initiator,  $"{FgaTypes.TradeProposal}:{proposalId}"),
            T($"user:{Id()}",                                   FgaRelations.Recipient,  $"{FgaTypes.TradeProposal}:{proposalId}"),
            T($"{FgaTypes.Delegation}:{delegId}#active_delegator", FgaRelations.Supervisor, $"{FgaTypes.TradeProposal}:{proposalId}"));
    }
}
