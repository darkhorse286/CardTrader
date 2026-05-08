using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;

namespace CardTrader.Authorization.Tests.Adversarial;

/// <summary>
/// Shared helpers for all adversarial test classes.
/// Each subclass creates its own store so tuple state never bleeds between classes.
/// </summary>
public abstract class FgaTestBase : IAsyncLifetime
{
    private readonly OpenFgaFixture _fixture;
    protected OpenFgaClient Client { get; private set; } = null!;

    protected FgaTestBase(OpenFgaFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync() =>
        Client = await _fixture.CreateStoreWithModelAsync(GetType().Name);

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Tuple write helpers ──────────────────────────────────────────────────

    protected Task WriteAsync(params ClientTupleKey[] tuples) =>
        Client.Write(new ClientWriteRequest { Writes = [..tuples] });

    protected Task DeleteAsync(params ClientTupleKeyWithoutCondition[] tuples) =>
        Client.Write(new ClientWriteRequest { Deletes = [..tuples] });

    protected static ClientTupleKey T(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };

    protected static ClientTupleKey TWithCondition(
        string user, string relation, string obj,
        string conditionName, object conditionContext) =>
        new()
        {
            User = user, Relation = relation, Object = obj,
            Condition = new RelationshipCondition
            {
                Name = conditionName,
                Context = conditionContext,
            },
        };

    protected static ClientTupleKeyWithoutCondition D(string user, string relation, string obj) =>
        new() { User = user, Relation = relation, Object = obj };

    // ── Check helpers ────────────────────────────────────────────────────────

    protected async Task<bool> CanAsync(string user, string relation, string obj) =>
        (await Client.Check(new ClientCheckRequest
        {
            User = user, Relation = relation, Object = obj,
        })).Allowed == true;

    protected async Task<bool> CanAsync(string user, string relation, string obj, object context) =>
        (await Client.Check(new ClientCheckRequest
        {
            User = user, Relation = relation, Object = obj, Context = context,
        })).Allowed == true;

    // ── ID generator ────────────────────────────────────────────────────────

    protected static string Id() => Guid.NewGuid().ToString("N")[..8];
}
