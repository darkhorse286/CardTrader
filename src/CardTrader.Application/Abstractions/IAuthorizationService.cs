namespace CardTrader.Application.Abstractions;

/// <summary>
/// The sole authorization entry point for the Application layer.
/// Implementations live in CardTrader.Authorization; this assembly never references the OpenFGA SDK.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Returns true when <paramref name="user"/> holds <paramref name="relation"/>
    /// on <paramref name="object"/> (formatted as "type:id", e.g. "card_instance:abc").
    /// </summary>
    Task<bool> CheckAsync(
        string user,
        string relation,
        string @object,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all object IDs (formatted as "type:id") on which
    /// <paramref name="user"/> holds <paramref name="relation"/>.
    /// </summary>
    Task<IReadOnlyList<string>> ListObjectsAsync(
        string user,
        string relation,
        string type,
        CancellationToken cancellationToken = default);
}
