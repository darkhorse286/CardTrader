using CardTrader.Domain.Entities;

namespace CardTrader.Application.Services;

public sealed record PackResult(
    bool IsGodPack,
    IReadOnlyList<(Card Card, CardInstance Instance)> Cards);
