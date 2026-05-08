namespace CardTrader.Domain.ValueObjects;

public readonly record struct DelegationId(Guid Value)
{
    public static DelegationId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
