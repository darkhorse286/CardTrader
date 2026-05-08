namespace CardTrader.Domain.ValueObjects;

public readonly record struct TradeProposalId(Guid Value)
{
    public static TradeProposalId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
