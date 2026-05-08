using CardTrader.Application.Abstractions;

namespace CardTrader.Application.Services;

public sealed class CardAccessService(IAuthorizationService authz)
{
    public Task<bool> CanViewAsync(string userId, string cardInstanceId, CancellationToken ct = default) =>
        authz.CheckAsync($"user:{userId}", "can_view", $"card_instance:{cardInstanceId}", ct);

    public Task<bool> CanManageAsync(string userId, string cardInstanceId, CancellationToken ct = default) =>
        authz.CheckAsync($"user:{userId}", "can_manage", $"card_instance:{cardInstanceId}", ct);

    public Task<bool> CanTradeAsync(string userId, string cardInstanceId, CancellationToken ct = default) =>
        authz.CheckAsync($"user:{userId}", "can_trade", $"card_instance:{cardInstanceId}", ct);
}
